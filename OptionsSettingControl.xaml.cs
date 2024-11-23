using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MAKE
{
    /// <summary>
    /// OptionsSettingControl.xaml
    /// </summary>
    public partial class OptionsSettingControl : UserControl
    {
        private Config config;

        public class TargetMachineItem
        {
            public string Display { get; set; }
            public string User { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
        }
        public List<TargetMachineItem> TargetMachineItems;

        public SortedDictionary<string, List<string>> IntelliSenseDirectorys = new SortedDictionary<string, List<string>>();

        public OptionsSettingControl(Config config)
        {
            InitializeComponent();
            this.config = config;
            Load();
        }

        public void OnClickOK(object sender, RoutedEventArgs e)
        {
            if (this.TargetMachine.SelectedIndex == -1)
            {
                MessageBox.Show((System.Windows.Window)this.Parent, 
                    "没有设置 \"远程主机\"", "Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.TargetMachine.Focus();
                return;
            }
            if (this.WorkingDirectory.Text.Length == 0)
            {
                MessageBox.Show((System.Windows.Window)this.Parent,
                    "没有设置 \"远程工作目录\"", "Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.WorkingDirectory.Focus();
                return;
            }

            Save();
            ((System.Windows.Window)this.Parent).DialogResult = true;
            ((System.Windows.Window)this.Parent).Close();
        }

        public void OnClickCancel(object sender, RoutedEventArgs e)
        {
            ((System.Windows.Window)this.Parent).Close();
        }

        public void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                this.IntelliSenseDirectorys[(string)e.RemovedItems[0]] = ParsePath(this.CodeDirectory.Text);
            }
            if (e.AddedItems.Count > 0)
            {
                this.CodeDirectory.Text = string.Join("\r\n", this.IntelliSenseDirectorys[(string)e.AddedItems[0]]);
            }
        }

        public void Load()
        {
            this.TargetMachine.ItemsSource = SSHLaunchOptions.GetConnectionList();
            this.TargetMachine.Text = this.config.target_machine;
            this.WorkingDirectory.Text = this.config.target_working_directory;
            this.TerminalType.Text = this.config.terminal_type;
            foreach (var key_value in this.config.intellisense_directory)
            {
                this.IntelliSenseDirectorys[key_value.Key] = new List<string>(key_value.Value);
                this.IntelliSenseEnvironment.Items.Add(key_value.Key);
            }
            this.IntelliSenseEnvironment.Text = this.config.intellisense_environment;
        }

        public void Save()
        {
            this.config.target_machine = (string)this.TargetMachine.SelectedItem;
            this.config.target_working_directory = this.WorkingDirectory.Text;
            this.config.terminal_type = this.TerminalType.Text;
            this.IntelliSenseDirectorys[this.IntelliSenseEnvironment.Text] = ParsePath(this.CodeDirectory.Text);
            this.config.intellisense_environment = this.IntelliSenseEnvironment.Text;
            this.config.intellisense_directory = this.IntelliSenseDirectorys;
        }

        private static List<string> ParsePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new List<string>();
            }
            string[] sep =  { "\r\n" };
            return new List<string>(path.Split(sep, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
