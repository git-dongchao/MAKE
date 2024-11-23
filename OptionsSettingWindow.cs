using System.Windows;

namespace MAKE
{
    public class OptionsSettingWindow : System.Windows.Window
    {
        public OptionsSettingWindow(Config config)
        {
            this.Title = "设置";
            //this.Width = 450;
            //this.Height = 250;
            this.Width = 600;
            this.Height = 400;
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            this.Content = new OptionsSettingControl(config);
        }
    }
}
