using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using SharpDX;
using ImGuiVector2 = System.Numerics.Vector2;

namespace PassiveSkillTreePlanter
{
    public class PassiveSkillTreePlanterSettings : SettingsBase
    {
        public PassiveSkillTreePlanterSettings()
        {
            var centerPos = BasePlugin.API.GameController.Window.GetWindowRectangle().Center;
            Enable = false;
            PickedBorderColor = new ColorNode(Color.Gray);
            UnpickedBorderColor = new ColorNode(Color.Green);
            WrongPickedBorderColor = new ColorNode(Color.Red);
            LineWidth = new RangeNode<int>(3, 0, 5);
            LineColor = new ColorNode(new Color(0, 255, 0, 50));
            ShowWindow = false;
            SelectedURLFile = string.Empty;
            LastSettingSize = new ImGuiVector2(620, 376);
            LastSettingPos = new ImGuiVector2(centerPos.X - LastSettingSize.X / 2, centerPos.Y - LastSettingSize.Y / 2);
        }

        public RangeNode<int> PickedBorderWidth { get; set; } = new RangeNode<int>(1, 1, 5);
        public RangeNode<int> UnpickedBorderWidth { get; set; } = new RangeNode<int>(3, 1, 5);
        public RangeNode<int> WrongPickedBorderWidth { get; set; } = new RangeNode<int>(3, 1, 5);

        public ColorNode PickedBorderColor { get; set; }
        public ColorNode UnpickedBorderColor { get; set; }
        public ColorNode WrongPickedBorderColor { get; set; }

        public RangeNode<int> LineWidth { get; set; }

        public ColorNode LineColor { get; set; }

        public string SelectedURLFile { get; set; }

        [Menu("Show ImGui Settings")]
        public ToggleNode ShowWindow { get; set; }

        public ImGuiVector2 LastSettingPos { get; set; }
        public ImGuiVector2 LastSettingSize { get; set; }
    }
}