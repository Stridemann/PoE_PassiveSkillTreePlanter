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
            UpdateTreeData = new ButtonNode();
            UrlFileList = new ListNode();
        }

        [Menu("Borders Width")]
        public RangeNode<int> BorderWidth { get; set; }

        [Menu("Borders Color")]
        public ColorNode BorderColor { get; set; }

        [Menu("Lines Width")]
        public RangeNode<int> LineWidth { get; set; }

        [Menu("Lines Color")]
        public ColorNode LineColor { get; set; }

        //[Menu("Download/Update skill tree data")] //Disabled
        public ButtonNode UpdateTreeData { get; set; }

        [Menu("Url's")]
        public ListNode UrlFileList { get; set; }
    }
}