using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using SharpDX;

namespace PassiveSkillTreePlanter
{
    public class PassiveSkillTreePlanter_Settings : SettingsBase
    {
        public PassiveSkillTreePlanter_Settings()
        {
            Enable = false;
            BorderWidth = new RangeNode<int>(1, 1, 5);
            BorderColor = new ColorNode(Color.Red);
            LineWidth = new RangeNode<int>(3, 0, 5);
            LineColor = new ColorNode(new Color(0, 255, 0, 50));
            UpdateTree = false;
        }

        [Menu("Border Width")]
        public RangeNode<int> BorderWidth { get; set; }

        [Menu("Border Color")]
        public ColorNode BorderColor { get; set; }

        [Menu("Lines Width")]
        public RangeNode<int> LineWidth { get; set; }

        [Menu("Lines Color")]
        public ColorNode LineColor { get; set; }

        [Menu("Download/Update skill tree data")]
        public ToggleNode UpdateTree { get; set; }
    }
}