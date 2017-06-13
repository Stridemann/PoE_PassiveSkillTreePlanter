using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace PassiveSkillTreePlanter
{
    public class PassiveSkillTreePlanter_Settings : SettingsBase
    {
        public PassiveSkillTreePlanter_Settings()
        {
            Enable = false;
            BorderWidth = new RangeNode<int>(1, 1, 5);
            BorderColor = new ColorNode(SharpDX.Color.Red);
            UpdateTree = false;
        }

        [Menu("Border Width")]
        public RangeNode<int> BorderWidth { get; set; }

        [Menu("Border Color")]
        public ColorNode BorderColor { get; set; }

        [Menu("Download/Update skill tree data")]
        public ToggleNode UpdateTree { get; set; }
    }
}