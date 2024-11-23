using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System.Windows;
using System.Threading;

namespace MAKE
{
    public class Bind
    {
        public Bind()
        {
        }

        public void Load(string filename)
        {
            if (this.assembly == null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly.FullName.StartsWith("Microsoft.VisualStudio.Terminal.Implementation"))
                    {
                        this.assembly = assembly;
                        break;
                    }
                }
            }
        }

        public Bind GetType(string type)
        {
            Bind bind = new Bind();
            bind.assembly = this.assembly;
            bind.type = this.assembly.GetType(type);
            return bind;
        }

        public Bind GetType(object obj)
        {
            Bind bind = new Bind();
            bind.assembly = this.assembly;
            bind.type = obj.GetType();
            return bind;
        }

        public object New(object[] args)
        {
            Type[] types = new Type[args.Length];
            for (int i = 0; i < args.Length; ++i)
            {
                types[i] = args[i].GetType();
            }
            ConstructorInfo constructor = this.type.GetConstructor(types);
            return constructor.Invoke(args);
        }

        public object GetProp(object self, string name)
        {
            PropertyInfo property = null;
            if (self != null)
            {
                property = this.type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            else
            {
                property = this.type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            return property.GetValue(self);
        }

        public void SetProp(object self, string name, object value)
        {
            PropertyInfo property = null;
            if (self != null)
            {
                property = this.type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            else
            {
                property = this.type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            property.SetValue(self, value);
        }

        public object GetField(object self, string name)
        {
            FieldInfo field = null;
            if (self != null)
            {
                field = this.type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            else
            {
                field = this.type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            return field.GetValue(self);
        }

        public object Call(object self, string name, object[] args)
        {
            MethodInfo method = null;
            if (self != null)
            {
                method = this.type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            else
            {
                method = this.type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            return method.Invoke(self, args);
        }

        private Type type = null;
        private Assembly assembly = null;
    }

    class VSTerminal
    {
        public static bool IsSSHEnable(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
            if (File.Exists(path))
            {
                string extension = Path.GetExtension(path);
                if (extension == ".sh" || extension == ".py" || extension == "")
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsCMDEnable(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
            if (File.Exists(path))
            {
                string extension = Path.GetExtension(path);
                if (extension == ".bat" || extension == ".exe" || extension == ".py")
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task<ToolWindowPane> CreateSSHTerminalAsync(string path, string args = "", string envs = "")
        {
            string file_name = Config.GetFileName(path);
            SSHLaunchOptions launch = Global.config.CreateSSHLaunchOptions();
            ///< arguments
            string arguments;
            if (!String.IsNullOrEmpty(file_name))
            {
                if (String.IsNullOrEmpty(args))
                {
                    arguments = string.Format("-Z {0} -p {1} -t -t {2}@{3} \"cd \"{4}\" ; clear ; {5} ; ./{6} ; bash\"",
                        launch.GetPassword(),
                        launch.GetPort(),
                        launch.GetUser(),
                        launch.GetHost(),
                        Global.config.GetLinuxDirectory(path),
                        envs,
                        file_name); ;
                }
                else
                {
                    arguments = string.Format("-Z {0} -p {1} -t -t {2}@{3} \"cd \"{4}\" ; clear ; {5} ; ./{6} {7} ; bash\"",
                        launch.GetPassword(),
                        launch.GetPort(),
                        launch.GetUser(),
                        launch.GetHost(),
                        Global.config.GetLinuxDirectory(path),
                        envs,
                        file_name,
                        args.Replace("\"", "\\\""));
                }
            }
            else
            {
                arguments = string.Format("-Z {0} -p {1} -t -t {2}@{3} \"cd \"{4}\" ; {5} ;bash\"",
                launch.GetPassword(),
                launch.GetPort(),
                launch.GetUser(),
                launch.GetHost(),
                Global.config.GetLinuxDirectory(path),
                envs);
            }
            string title = $"SSH({launch.GetUser()}@{launch.GetHost()}:{launch.GetPort()})";

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return await CreateTerminalImplAsync(title, VSHelper.GetSshExePath(), arguments);
        }

        public static async Task<ToolWindowPane> CreateSSHTerminalCommandAsync(string remote_directory, string command)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            SSHLaunchOptions launch = Global.config.CreateSSHLaunchOptions();
            ///< arguments
            string arguments;
            if (command != null && command.Length > 0)
            {
                arguments = string.Format("-Z {0} -p {1} -t -t {2}@{3} \"cd \"{4}\" ; {5}\"",
                launch.GetPassword(),
                launch.GetPort(),
                launch.GetUser(),
                launch.GetHost(),
                remote_directory,
                command);
            }
            else
            {
                arguments = string.Format("-Z {0} -p {1} -t -t {2}@{3} \"cd \"{4}\" ; bash\"",
                launch.GetPassword(),
                launch.GetPort(),
                launch.GetUser(),
                launch.GetHost(),
                remote_directory);
            }

            string title = $"SSH({launch.GetUser()}@{launch.GetHost()}:{launch.GetPort()})";
            return await CreateTerminalImplAsync(title, VSHelper.GetSshExePath(), arguments);
        }

        public static async Task<ToolWindowPane> CreateCMDTerminalAsync(string path, string args = "", string envs = "")
        {
            string working_directory = Config.GetDirectoryName(path);
            string file_name = Config.GetFileName(path);
            string arguments = $"/k ";
            if (file_name != null && file_name.Length > 0)
            {
                if (String.IsNullOrEmpty(args))
                {
                    arguments += $"cd /d \"{working_directory}\" & {envs} & {file_name}";
                }
                else
                {
                    arguments += $"cd /d \"{working_directory}\" & {envs} & {file_name} {args}";
                }
            }
            else
            {
                arguments += $"cd /d \"{working_directory}\"";
            }

            return await CreateTerminalImplAsync("CMD", "cmd", arguments);
        }

        public static void CloseTerminal(ToolWindowPane terminal)
        {
            VSTerminal.TerminalWindowPackage pkg = new VSTerminal.TerminalWindowPackage((AsyncPackage)VSHelper.GetTerminalWindowPackage());
            pkg.CloseToolWindow(terminal);
        }

        private static async Task<ToolWindowPane> CreateTerminalImplAsync(string title, string file, string arguments)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            IVsPackage vs_package = VSHelper.GetTerminalWindowPackage();
            VSTerminal.TerminalWindowPackage pkg = new VSTerminal.TerminalWindowPackage((AsyncPackage)vs_package);
            ToolWindowPane tool_window = await pkg.CreateToolWindowAsync(new OptionsProfileConfig("",
                title,
                file,
                arguments, false));
            return tool_window;
        }

        private class OptionsProfileConfig
        {
            public OptionsProfileConfig(string id, string displayName, string location, string arguments = null, bool isDefault = false)
            {
                this.profile_config = termina_bind.GetType("Microsoft.VisualStudio.Terminal.OptionsProfileConfig");

                this.Id = id;
                this.DisplayName = displayName;
                this.Location = location;
                this.Arguments = arguments;
                this.IsDefault = isDefault;

                this.profile = this.profile_config.New(new object[] { id, displayName, location, arguments, IsDefault });
            }

            public object Get()
            {
                this.profile_config.SetProp(this.profile, "Id", this.Id);
                this.profile_config.SetProp(this.profile, "DisplayName", this.DisplayName);
                this.profile_config.SetProp(this.profile, "Location", this.Location);
                this.profile_config.SetProp(this.profile, "Arguments", this.Arguments);
                this.profile_config.SetProp(this.profile, "IsDefault", this.IsDefault);
                return this.profile;
            }

            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string Location { get; set; }
            public string Arguments { get; set; }
            public bool IsDefault { get; set; }

            private object profile = null;
            private Bind profile_config = null;
        }

        private class TerminalSettingsManager
        {
            public static string ComputeProfileSha(string path, string arg, string name)
            {
                string s = path.Trim() + arg.Trim() + name.Trim();
                SHA256 sHA = SHA256.Create();
                byte[] inArray = sHA.ComputeHash(Encoding.Unicode.GetBytes(s));
                _ = string.Empty;
                return Convert.ToBase64String(inArray);
            }

            public static string AddProfile(OptionsProfileConfig profile)
            {
                Bind terminal_settings_manager = termina_bind.GetType("Microsoft.VisualStudio.Terminal.ToolsOptions.TerminalSettingsManager"); 
                object self = terminal_settings_manager.GetProp(null, "ProfilesCache");
                terminal_settings_manager.GetType(self).Call(self, "Add", new object[] { profile.Get() });
                return profile.Id;
            }

            public static bool RemoveProfile(OptionsProfileConfig profile)
            {
                Bind terminal_settings_manager = termina_bind.GetType("Microsoft.VisualStudio.Terminal.ToolsOptions.TerminalSettingsManager");
                object self = terminal_settings_manager.GetProp(null, "ProfilesCache");
                return (bool)terminal_settings_manager.GetType(self).Call(self, "Remove", new object[] { profile.Get() });
            }
        }

        private class WindowStateStorage
        {
            public WindowStateStorage(object obj)
            {
                this.window_state_storage = termina_bind.GetType("Microsoft.VisualStudio.Terminal.WindowStateStorage");
                this.obj = obj;
            }

            public int ActiveWindows()
            {
                return (int)this.window_state_storage.Call(this.obj, "ActiveWindows", null);
            }
            public void AddWindow(int id, string profileId)
            {
                this.window_state_storage.Call(this.obj, "AddWindow", new object[] { id, profileId });
            }
            public int GetNewID()
            {
                return (int)this.window_state_storage.Call(this.obj, "GetNewID", null);
            }
            public string GetProfileFromWindowId(int windowId)
            {
                return (string)this.window_state_storage.Call(this.obj, "GetProfileFromWindowId", new object[] { windowId });
            }
            public void LoadData()
            {
                this.window_state_storage.Call(this.obj, "LoadData", null);
            }
            public void RemoveWindow(int id)
            {
                this.window_state_storage.Call(this.obj, "RemoveWindow", new object[] { id });
            }
            public void SaveData()
            {
                this.window_state_storage.Call(this.obj, "SaveData", null);
            }

            private object obj;
            private Bind window_state_storage;
        }

        private class TerminalControl
        {
            public TerminalControl(object control)
            {
                this.terminal_control = termina_bind.GetType("Microsoft.VisualStudio.Terminal.TerminalControl");
                this.control = control;
            }

            public System.Threading.Tasks.Task WaitForInitAsync()
            {
                return (System.Threading.Tasks.Task)this.terminal_control.Call(this.control, "WaitForInitAsync", null);
            }

            object control = null;
            private Bind terminal_control = null;
        }

        private class TerminalWindowPackage
        {
            public TerminalWindowPackage(AsyncPackage package)
            {
                termina_bind.Load("Microsoft.VisualStudio.Terminal.Implementation");

                terminal_package = termina_bind.GetType("Microsoft.VisualStudio.Terminal.TerminalWindowPackage");

                object self = terminal_package.GetProp(package, "WindowStateStorage");
                this.state_storage = new WindowStateStorage(self);
                this.package = package;
            }

            public async Task<ToolWindowPane> CreateToolWindowAsync(OptionsProfileConfig profile)
            {
                try
                {
                    string profile_id = TerminalSettingsManager.AddProfile(profile);
                    int newID = this.state_storage.GetNewID();
                    this.state_storage.AddWindow(newID, profile_id);

                    Task<object> task = terminal_package.Call(this.package, "CreateToolWindowAsync", new object[] { newID, true }) as Task<object>;

                    ToolWindowPane tool_window = await task.ConfigureAwait(true) as ToolWindowPane;
                    TerminalControl control = new TerminalControl(tool_window.Content);
                    await ThreadingTools.WithCancellation(control.WaitForInitAsync(), (CancellationToken)terminal_package.GetProp(this.package, "DisposalToken"));
                    TerminalSettingsManager.RemoveProfile(profile);
                    return tool_window;
                }
                catch (Exception ex)
                {
                    VSHelper.ShowMessageBox("", ex.Message, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGICON.OLEMSGICON_WARNING);
                }
                return null;
            }

            public void CloseToolWindow(ToolWindowPane window_pane)
            {
                Bind terminal_window = termina_bind.GetType("Microsoft.VisualStudio.Terminal.TerminalWindow");
                {
                    object ptyProxy = terminal_window.GetField(window_pane, "ptyProxy");
                    System.Threading.Tasks.Task task = termina_bind.GetType("Microsoft.VisualStudio.Terminal.IServer").Call(ptyProxy, "ClosePtyAsync", null) as System.Threading.Tasks.Task;
                    task.Wait();
                }
                {
                    System.Threading.Tasks.Task task = terminal_window.Call(window_pane, "CloseAsync", new object[] { terminal_package.GetProp(this.package, "DisposalToken") }) as System.Threading.Tasks.Task;
                    task.Wait();
                }
            }

            private WindowStateStorage state_storage;
            private object package;
            private Bind terminal_package;
        }

        private static Bind termina_bind = new Bind();
    }
}
