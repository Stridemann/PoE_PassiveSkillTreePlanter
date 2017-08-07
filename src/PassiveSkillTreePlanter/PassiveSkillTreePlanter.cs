using System.Collections.Generic;
using PoeHUD.Plugins;
using System.IO;
using SharpDX;
using PoeHUD.Poe;
using PassiveSkillTreePlanter.UrlDecoders;
using PassiveSkillTreePlanter.SkillTreeJson;
using PoeHUD.Framework;
using System.Linq;

namespace PassiveSkillTreePlanter
{
    public class PassiveSkillTreePlanter : BaseSettingsPlugin<PassiveSkillTreePlanter_Settings>
    {
        private List<SkillNode> DrawNodes = new List<SkillNode>();
        private PoESkillTreeJsonDecoder SkillTreeeData = new PoESkillTreeJsonDecoder();

        private Element UiSkillTreeBase;
        private List<ushort> UrlNodes = new List<ushort>();//List of nodes decoded from URL

        private const string SkillTreeDataFile = "SkillTreeData.dat";
        private const string SkillTreeDir = "Builds";

        private string SkillTreeUrlFilesDir => LocalPluginDirectory + @"\" + SkillTreeDir;

        public override void Initialise()
        {
            Settings.UpdateTreeData.OnPressed += delegate
            {
                DownloadTree();
            };

            Settings.UrlFileList.OnValueSelected += ReadUrlFromSelectedUrl;

            if (!Directory.Exists(SkillTreeUrlFilesDir))
            {
                Directory.CreateDirectory(SkillTreeUrlFilesDir);
                LogMessage("PassiveSkillTree: Write your skill tree url to txt file and place it to Builds folder.", 10);
                return;
            }

            //Update url list variants
            var dirInfo = new DirectoryInfo(SkillTreeUrlFilesDir);
            var urlVariants = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
            Settings.UrlFileList.SetListValues(urlVariants);


            //Read url
            ReadUrlFromSelectedUrl(Settings.UrlFileList.Value);

            Graphics.Render += ExtRender;
        }

    

        private void ReadUrlFromSelectedUrl(string fileName)
        {
            var skillTreeUrlFilePath = Path.Combine(SkillTreeUrlFilesDir, fileName + ".txt");

            if (!File.Exists(skillTreeUrlFilePath))
            {
                LogMessage("PassiveSkillTree: Select build url from list in options.", 10);
                return;
            }

            string skillTreeUrl = File.ReadAllText(skillTreeUrlFilePath);

            if (!DecodeUrl(skillTreeUrl))
            {
                LogMessage("PassiveSkillTree: Can't decode url from file: " + skillTreeUrlFilePath, 10);
                return;
            }
 
            ProcessNodes();
        }

        private void ProcessNodes()
        {
            DrawNodes = new List<SkillNode>();

            //Read data
            var skillTreeDataPath = LocalPluginDirectory + @"\" + SkillTreeDataFile;
            if (!File.Exists(skillTreeDataPath))
            {
                LogMessage("PassiveSkillTree: Can't find file " + SkillTreeDataFile + " with skill tree data.", 10);
                return;
            }

            string skillTreeJson = File.ReadAllText(skillTreeDataPath);
            SkillTreeeData.Decode(skillTreeJson);

            foreach (var urlNodeId in UrlNodes)
            {
                if (!SkillTreeeData.Skillnodes.ContainsKey(urlNodeId))
                {
                    LogError("PassiveSkillTree: Can't find passive skill tree node with id: " + urlNodeId, 5);
                    continue;
                }
                var node = SkillTreeeData.Skillnodes[urlNodeId];
                node.Init();
                DrawNodes.Add(node);


                List<ushort> dontDrawLinesTwice = new List<ushort>();

                foreach (var lNodeId in node.linkedNodes)
                {
                    if (!UrlNodes.Contains(lNodeId)) continue;
                    if (dontDrawLinesTwice.Contains(lNodeId)) continue;

                    if (!SkillTreeeData.Skillnodes.ContainsKey(lNodeId))
                    {
                        LogError("PassiveSkillTree: Can't find passive skill tree node with id: " + lNodeId + " to draw the link", 5);
                        continue;
                    }
                    var lNode = SkillTreeeData.Skillnodes[lNodeId];

                    node.DrawNodeLinks.Add(lNode.Position);
                }

                dontDrawLinesTwice.Add(urlNodeId);
            }
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
            if (DrawNodes.Count == 0) return;

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

            var scale = UiSkillTreeBase.Scale;


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
                        Graphics.DrawLine(new Vector2(posX, posY), new Vector2(linkDrawPosX, linkDrawPosY), Settings.LineWidth, Settings.LineColor);
                    }
                }
            }
        }

    }
}