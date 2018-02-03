using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ImGuiNET;
using PassiveSkillTreePlanter.SkillTreeJson;
using PassiveSkillTreePlanter.UrlDecoders;
using PoeHUD.Framework;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using SharpDX;

namespace PassiveSkillTreePlanter
{
    public class PassiveSkillTreePlanter : BaseSettingsPlugin<PassiveSkillTreePlanterSettings>
    {
        private const string SkillTreeDataFile = "SkillTreeData.dat";
        private const string SkillTreeDir = "Builds";
        private readonly PoESkillTreeJsonDecoder _skillTreeeData = new PoESkillTreeJsonDecoder();


        private bool _bUiRootInitialized;
        private List<SkillNode> _drawNodes = new List<SkillNode>();

        private Element _uiSkillTreeBase;
        private List<ushort> _urlNodes = new List<ushort>(); //List of nodes decoded from URL

        public string AddNewBuildFile = "";
        public string AddNewBuildUrl = "";
        public string CurrentlySelectedBuildFile { get; set; }
        public string CurrentlySelectedBuildUrl { get; set; }

        private string SkillTreeUrlFilesDir => LocalPluginDirectory + @"\" + SkillTreeDir;
        private List<string> BuildFiles { get; set; }
        public IntPtr TextEditCallback { get; set; }

        public override void Initialise()
        {
            if (!Directory.Exists(SkillTreeUrlFilesDir))
            {
                Directory.CreateDirectory(SkillTreeUrlFilesDir);
                LogMessage("PassiveSkillTree: Write your skill tree url to txt file and place it to Builds folder.", 10);
                return;
            }

            LoadBuildFiles();

            //Read url
            ReadUrlFromSelectedUrl(Settings.SelectedURLFile);
        }

        public override void Render()
        {
            base.Render();
            ExtRender();
            ImGuiMenu();
        }

        private void LoadBuildFiles()
        {
            //Update url list variants
            var dirInfo = new DirectoryInfo(SkillTreeUrlFilesDir);
            BuildFiles = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
        }

        private static string CleanFileName(string fileName) { return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty)); }

        public void RenameFile(string fileName, string oldFileName)
        {
            fileName = CleanFileName(fileName);
            var newFilePath = Path.Combine(SkillTreeUrlFilesDir, fileName + ".txt");
            var oldFilePath = Path.Combine(SkillTreeUrlFilesDir, oldFileName + ".txt");
            if (File.Exists(newFilePath))
            {
                LogError("PassiveSkillTreePlanter: File already Exists!", 10);
                return;
            }

            //Now Rename the File
            File.Move(oldFilePath, newFilePath);
            Settings.SelectedURLFile = fileName;
            ReadUrlFromSelectedUrl(Settings.SelectedURLFile);
        }

        public void ReplaceUrlContents(string fileName, string newContents)
        {
            fileName = CleanFileName(fileName);
            var filePath = Path.Combine(SkillTreeUrlFilesDir, fileName + ".txt");
            if (!File.Exists(filePath))
            {
                LogError("PassiveSkillTreePlanter: File doesnt exist", 10);
                return;
            }

            if (!IsBase64String(newContents))
            {
                LogError("PassiveSkillTreePlanter: Invalid URL or you are trying to add something that is not a pathofexile.com build URL", 10);
                return;
            }

            //Now Rename the File
            File.WriteAllText(filePath, newContents);
            ReadUrlFromSelectedUrl(Settings.SelectedURLFile);
        }

        public void AddNewBuild(string buildName, string buildUrl)
        {
            var newFilePath = Path.Combine(SkillTreeUrlFilesDir, buildName + ".txt");
            if (File.Exists(newFilePath))
            {
                LogError("PassiveSkillTreePlanter.Add: File already Exists!", 10);
                return;
            }

            if (!IsBase64String(buildUrl))
            {
                LogError("PassiveSkillTreePlanter.Add: Invalid URL or you are trying to add something that is not a pathofexile.com build URL OR you need to remove the game version (3.x.x)", 10);
                return;
            }

            File.WriteAllText(newFilePath, buildUrl);
            LoadBuildFiles();
        }

        public static bool IsBase64String(string url)
        {
            try
            {
                url = url.Split('/').LastOrDefault()?.Replace("-", "+").Replace("_", "/");
                // If no exception is caught, then it is possibly a base64 encoded string
                var data = Convert.FromBase64String(url);
                // The part that checks if the string was properly padded to the
                return url.Replace(" ", "").Length % 4 == 0;
            }
            catch
            {
                // If exception is caught, then it is not a base64 encoded string
                return false;
            }
        }

        private void ImGuiMenu()
        {
            if (!Settings.ShowWindow) return;
            ImGuiExtension.BeginWindow($"{PluginName} Settings", Settings.LastSettingPos.X, Settings.LastSettingPos.Y, Settings.LastSettingSize.X, Settings.LastSettingSize.Y);
            if (ImGui.Button("Open Build Folder")) Process.Start(SkillTreeUrlFilesDir);
            ImGui.SameLine();
            if (ImGui.Button("Reload List")) LoadBuildFiles();
            Settings.SelectedURLFile = ImGuiExtension.ComboBox("Build Files", Settings.SelectedURLFile, BuildFiles, out var tempBool, ComboFlags.HeightLarge);
            if (tempBool) ReadUrlFromSelectedUrl(Settings.SelectedURLFile);
            //if (ImGui.Button("Download/Update skill tree data")) DownloadTree(); // Disabled
            if (ImGui.CollapsingHeader("Colors", "Colors", false, true))
            {
                ImGuiNative.igIndent(16f);
                Settings.BorderColor.Value = ImGuiExtension.ColorPicker("Border Color", Settings.BorderColor);
                Settings.LineColor.Value = ImGuiExtension.ColorPicker("Line Color", Settings.LineColor);
                ImGuiNative.igUnindent(16f);
            }

            if (ImGui.CollapsingHeader("Sliders", "Sliders", true, true))
            {
                ImGuiNative.igIndent(16f);
                Settings.BorderWidth.Value = ImGuiExtension.IntSlider("Border Width", Settings.BorderWidth);
                Settings.LineWidth.Value = ImGuiExtension.IntSlider("Line Width", Settings.LineWidth);
                ImGuiNative.igUnindent(16f);
            }

            if (ImGui.CollapsingHeader("Selected Build Edit", "Selected Build Edit", false, false))
            {
                ImGuiNative.igIndent(16f);
                if (!string.IsNullOrEmpty(CurrentlySelectedBuildFile))
                {
                    CurrentlySelectedBuildFile = ImGuiExtension.InputText("##RenameLabel", CurrentlySelectedBuildFile, 1024, InputTextFlags.EnterReturnsTrue);
                    ImGui.SameLine();
                    if (ImGui.Button("Rename Currently Selected Build")) RenameFile(CurrentlySelectedBuildFile, Settings.SelectedURLFile);
                    CurrentlySelectedBuildUrl = ImGuiExtension.InputText("##ChangeURL", CurrentlySelectedBuildUrl, 1024, InputTextFlags.EnterReturnsTrue | InputTextFlags.AutoSelectAll);
                    ImGui.SameLine();
                    if (ImGui.Button("Change URL")) ReplaceUrlContents(Settings.SelectedURLFile, CurrentlySelectedBuildUrl);
                }
                else
                    ImGui.Text("No Build Selected");
                ImGuiNative.igUnindent(16f);
            }


            if (ImGui.CollapsingHeader("Add New Build", "Add New Build", false, false))
            {
                ImGuiNative.igIndent(16f);
                AddNewBuildFile = ImGuiExtension.InputText("Build Name", AddNewBuildFile, 1024, InputTextFlags.EnterReturnsTrue);
                AddNewBuildUrl = ImGuiExtension.InputText("Build URL", AddNewBuildUrl, 1024, InputTextFlags.EnterReturnsTrue | InputTextFlags.AutoSelectAll);
                if (ImGui.Button("Create Build"))
                {
                    AddNewBuild(AddNewBuildFile, AddNewBuildUrl);
                    AddNewBuildFile = "";
                    AddNewBuildUrl = "";
                }
            }

            // Storing window Position and Size changed by the user
            Settings.LastSettingPos = ImGui.GetWindowPosition();
            Settings.LastSettingSize = ImGui.GetWindowSize();
            ImGui.EndWindow();
        }

        private void ReadUrlFromSelectedUrl(string fileName)
        {
            var skillTreeUrlFilePath = Path.Combine(SkillTreeUrlFilesDir, fileName + ".txt");
            if (!File.Exists(skillTreeUrlFilePath))
            {
                LogMessage("PassiveSkillTree: Select build url from list in options.", 10);
                Settings.SelectedURLFile = string.Empty;
                return;
            }

            var skillTreeUrl = File.ReadAllText(skillTreeUrlFilePath);

            // replaces the game tree version "x.x.x/"
            var rgx = new Regex("^https:\\/\\/www.pathofexile.com\\/fullscreen-passive-skill-tree\\/(([0-9]\\W){3})", RegexOptions.IgnoreCase);
            var match = rgx.Match(skillTreeUrl);
            if (match.Success)
                skillTreeUrl = skillTreeUrl.Replace(match.Groups[1].Value, "");
            rgx = new Regex("^https:\\/\\/www.pathofexile.com\\/passive-skill-tree\\/(([0-9]\\W){3})", RegexOptions.IgnoreCase);
            match = rgx.Match(skillTreeUrl);
            if (match.Success)
                skillTreeUrl = skillTreeUrl.Replace(match.Groups[1].Value, "");
            if (!DecodeUrl(skillTreeUrl))
            {
                LogMessage("PassiveSkillTree: Can't decode url from file: " + skillTreeUrlFilePath, 10);
                return;
            }

            CurrentlySelectedBuildFile = fileName;
            CurrentlySelectedBuildUrl = File.ReadAllText(skillTreeUrlFilePath);
            ProcessNodes();
        }

        private void ProcessNodes()
        {
            _drawNodes = new List<SkillNode>();

            //Read data
            var skillTreeDataPath = LocalPluginDirectory + @"\" + SkillTreeDataFile;
            if (!File.Exists(skillTreeDataPath))
            {
                LogMessage("PassiveSkillTree: Can't find file " + SkillTreeDataFile + " with skill tree data.", 10);
                return;
            }

            var skillTreeJson = File.ReadAllText(skillTreeDataPath);
            _skillTreeeData.Decode(skillTreeJson);
            foreach (var urlNodeId in _urlNodes)
            {
                if (!_skillTreeeData.Skillnodes.ContainsKey(urlNodeId))
                {
                    LogError("PassiveSkillTree: Can't find passive skill tree node with id: " + urlNodeId, 5);
                    continue;
                }

                var node = _skillTreeeData.Skillnodes[urlNodeId];
                node.Init();
                _drawNodes.Add(node);
                var dontDrawLinesTwice = new List<ushort>();
                foreach (var lNodeId in node.linkedNodes)
                {
                    if (!_urlNodes.Contains(lNodeId)) continue;
                    if (dontDrawLinesTwice.Contains(lNodeId)) continue;
                    if (!_skillTreeeData.Skillnodes.ContainsKey(lNodeId))
                    {
                        LogError("PassiveSkillTree: Can't find passive skill tree node with id: " + lNodeId + " to draw the link", 5);
                        continue;
                    }

                    var lNode = _skillTreeeData.Skillnodes[lNodeId];
                    node.DrawNodeLinks.Add(lNode.Position);
                }

                dontDrawLinesTwice.Add(urlNodeId);
            }
        }


        private bool DecodeUrl(string url)
        {
            if (PoePlannerUrlDecoder.UrlMatch(url))
            {
                _urlNodes = PoePlannerUrlDecoder.Decode(url);
                return true;
            }

            if (PathOfExileUrlDecoder.UrlMatch(url))
            {
                _urlNodes = PathOfExileUrlDecoder.Decode(url);
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

        private void ExtRender()
        {
            if (_drawNodes.Count == 0) return;
            if (!GameController.InGame || !WinApi.IsForegroundWindow(GameController.Window.Process.MainWindowHandle)) return;
            var treePanel = GameController.Game.IngameState.IngameUi.TreePanel;
            if (!treePanel.IsVisible)
            {
                _bUiRootInitialized = false;
                _uiSkillTreeBase = null;
                return;
            }

            if (!_bUiRootInitialized)
            {
                _bUiRootInitialized = true;
                //I still can't find offset for Skill Tree root, so I made it by checking
                foreach (var child in treePanel.Children) //Only 8 childs for check
                    if (child.Width > 20000 && child.Width < 30000)
                    {
                        _uiSkillTreeBase = child;
                        break;
                    }
            }

            if (_uiSkillTreeBase == null)
            {
                LogError("Can't find UiSkillTreeBase root!", 0);
                return;
            }

            var scale = _uiSkillTreeBase.Scale;

            //Hand-picked values
            var offsetX = 12465;
            var offsetY = 11582;
            foreach (var node in _drawNodes)
            {
                var drawSize = node.DrawSize * scale;
                var posX = (_uiSkillTreeBase.X + node.DrawPosition.X + offsetX) * scale;
                var posY = (_uiSkillTreeBase.Y + node.DrawPosition.Y + offsetY) * scale;
                Graphics.DrawFrame(new RectangleF(posX - drawSize / 2, posY - drawSize / 2, drawSize, drawSize), Settings.BorderWidth, Settings.BorderColor);
                foreach (var link in node.DrawNodeLinks)
                {
                    var linkDrawPosX = (_uiSkillTreeBase.X + link.X + offsetX) * scale;
                    var linkDrawPosY = (_uiSkillTreeBase.Y + link.Y + offsetY) * scale;
                    if (Settings.LineWidth > 0) Graphics.DrawLine(new Vector2(posX, posY), new Vector2(linkDrawPosX, linkDrawPosY), Settings.LineWidth, Settings.LineColor);
                }
            }
        }
    }
}