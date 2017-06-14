using System.Collections.Generic;
using PoeHUD.Plugins;
using System.IO;
using SharpDX;
using PoeHUD.Poe;
using PassiveSkillTreePlanter.UrlDecoders;
using PassiveSkillTreePlanter.SkillTreeJson;
using PoeHUD.Framework;


namespace PassiveSkillTreePlanter
{
    public class PassiveSkillTreePlanter : BaseSettingsPlugin<PassiveSkillTreePlanter_Settings>
    {
        private List<SkillNode> DrawNodes = new List<SkillNode>();
        private PoESkillTreeJsonDecoder SkillTreeeData = new PoESkillTreeJsonDecoder();

        private Element UiSkillTreeBase;
        private List<ushort> UrlNodes = new List<ushort>();//List of nodes decoded from URL

        private const string SkillTreeDataFile = "SkillTreeData.dat";
        private const string SkillTreeUrlFile = "SkillTreeUrl.txt";


        public override void Initialise()
        {
            Settings.UpdateTree.OnValueChanged += delegate
            {
                DownloadTree();
            };

            var skillTreeUrlFilePath = LocalPluginDirectory + @"\" + SkillTreeUrlFile;

            if (!File.Exists(skillTreeUrlFilePath))
            {
                LogMessage("Write your skill tree url to " + SkillTreeUrlFile + " file.", 10);
                File.Create(skillTreeUrlFilePath);
                return;
            }

            string skillTreeUrl = File.ReadAllText(skillTreeUrlFilePath);

            if (!DecodeUrl(skillTreeUrl))
            {
                LogMessage("Write your skill tree url to " + SkillTreeUrlFile + " file.", 10);
                return;
            }

            var skillTreeDataPath = LocalPluginDirectory + @"\" + SkillTreeDataFile;
            if (!File.Exists(skillTreeDataPath))
            {
                LogMessage("Can't find file " + SkillTreeDataFile + " with skill tree data.", 10);
                return;
            }

            string skillTreeJson = File.ReadAllText(skillTreeDataPath);
            SkillTreeeData.Decode(skillTreeJson);


            foreach(var urlNodeId in UrlNodes)
            {
                if(!SkillTreeeData.Skillnodes.ContainsKey(urlNodeId))
                {
                    LogError("Can't find passive skill tree node with id: " + urlNodeId, 3);
                    continue;
                }
                var node = SkillTreeeData.Skillnodes[urlNodeId];
                node.Init();
                DrawNodes.Add(node);


                List<ushort> dontDrawLinesTwice = new List<ushort>();

                foreach (var lNodeId in node.linkedNodes)
                {
                    if (!UrlNodes.Contains(lNodeId)) continue;
                    if(dontDrawLinesTwice.Contains(lNodeId)) continue;

                    if (!SkillTreeeData.Skillnodes.ContainsKey(lNodeId))
                    {
                        LogError("Can't find passive skill tree node with id: " + lNodeId + " to draw the link", 3);
                        continue;
                    }
                    var lNode = SkillTreeeData.Skillnodes[lNodeId];

                    node.DrawNodeLinks.Add(lNode.Position);
                }

                dontDrawLinesTwice.Add(urlNodeId);
            }

            if (DrawNodes.Count > 0)
                Graphics.Render += ExtRender;

            CheckDrawLineMethod();
        }

        private System.Reflection.MethodInfo DrawLineMethod = null;
        private void CheckDrawLineMethod()
        {
            DrawLineMethod = typeof(PoeHUD.Hud.UI.Graphics).GetMethod("DrawLine");

            if (DrawLineMethod == null)
                LogMessage("Update PoeHud to see the lines between nodes.", 15);
        }
 

        private bool DecodeUrl(string url)
        {
            if (PoePlannerUrlDecoder.UrlMatch(url))
            {
                UrlNodes = PoePlannerUrlDecoder.Decode(url);
                return true;
            }
            else if (PathOfExileUrlDecoder.UrlMatch(url))
            {
                UrlNodes = PathOfExileUrlDecoder.Decode(url);
                return true;
            }
            return false;
        }

        private async void DownloadTree()
        {
            var skillTreeDataPath = LocalPluginDirectory + @"\" + SkillTreeDataFile;
            await PassiveSkillTreeJson_Downloader.DownloadSkillTreeToFileAsync(skillTreeDataPath);
            LogMessage("Skill tree updated!", 3);
        }


        private bool bUiRootInitialized = false;
        private void ExtRender()
        {
            if (!GameController.InGame || !WinApi.IsForegroundWindow(GameController.Window.Process.MainWindowHandle)) return; 


            var treePanel = GameController.Game.IngameState.IngameUi.TreePanel;
            if (!treePanel.IsVisible)
            {
                bUiRootInitialized = false;
                UiSkillTreeBase = null;
                return;
            }

            if (!bUiRootInitialized)
            {
                bUiRootInitialized = true;
                //I still can't find offset for Skill Tree root, so I made it by checking
                foreach (var child in treePanel.Children)//Only 8 childs for check
                {
                    if (child.Width > 20000 && child.Width < 30000)
                    {
                        UiSkillTreeBase = child;
                        break;
                    }
                }
            }

            if (UiSkillTreeBase == null)
            {
                LogError("Can't find UiSkillTreeBase root!", 0);
                return;
            }

            var scale = Memory.ReadFloat(UiSkillTreeBase.Address + 0x88c);


            //Hand-picked values
            var OffsetX = 12465;
            var OffsetY = 11582;

            foreach (var node in DrawNodes)
            {
                var DrawSize = node.DrawSize * scale;

                var posX = (UiSkillTreeBase.X + node.DrawPosition.X + OffsetX) * scale;
                var posY = (UiSkillTreeBase.Y + node.DrawPosition.Y + OffsetY) * scale;

                Graphics.DrawFrame(new RectangleF(posX - DrawSize / 2, posY - DrawSize / 2, DrawSize, DrawSize), Settings.BorderWidth, Settings.BorderColor);


                foreach (var link in node.DrawNodeLinks)
                {
                    var linkDrawPosX = (UiSkillTreeBase.X + link.X + OffsetX) * scale;
                    var linkDrawPosY = (UiSkillTreeBase.Y + link.Y + OffsetY) * scale;

                    if (Settings.LineWidth > 0)
                    {
                        if(DrawLineMethod != null)
                        {
                            DrawLineMethod.Invoke(Graphics, new object[] { new Vector2(posX, posY), new Vector2(linkDrawPosX, linkDrawPosY), (float)Settings.LineWidth.Value, Settings.LineColor.Value });
                        }
                        //Graphics.DrawLine(new Vector2(posX, posY), new Vector2(linkDrawPosX, linkDrawPosY), Settings.LineWidth, Settings.Lineolor);
                    }
                }
            }
        }

    }
}