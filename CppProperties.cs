using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;
using Microsoft.VisualStudio.Shell.Interop;
using CMakeParser;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.Win32;

namespace MAKE
{
    public class CMakeListsParser
    {
        public CMakeListsParser(string file)
        {
            // CMakeParser.Common.Builder.Instance
            CMakeParser.Core.ILogger logger = new CMakeParser.Common.Logger(new EmptyWriter());
            CMakeParser.Core.State state = new CMakeParser.Core.State(Path.GetDirectoryName(file), Path.GetDirectoryName(file));
            CMakeParser.Core.CMakeLists lists = new CMakeParser.Core.CMakeLists(state, logger);
            lists.AddCommand("set", new CMakeParser.Core.Set());
            lists.AddCommand("file", new CMakeParser.Core.File(logger));
            lists.AddCommand("include_directories", new IncludeDirectories(this));
            lists.Read();
        }

        public class EmptyWriter : CMakeParser.Common.IWriter
        {
            public void WriteLine(string line)
            {
            }
        }

        public class IncludeDirectories : CMakeParser.Core.ICommand
        {
            public IncludeDirectories(CMakeListsParser lists)
            {
                this.lists = lists;
                this.include_directories = new CMakeParser.Core.IncludeDirectories();
            }

            public void Initialise(CMakeParser.Core.State state)
            {
                include_directories.Initialise(state);
            }

            public void Command(KeyValuePair<string, string> command, CMakeParser.Core.State state)
            {
                var bits = CMakeParser.Core.Utilities.Split(command.Value, new string[] { "AFTER", "BEFORE", "SYSTEM" });
                var line = command.Value.Replace("AFTER", string.Empty).Replace("BEFORE", string.Empty).Replace("SYSTEM", string.Empty);
                line = state.Replace(line).Replace(Path.DirectorySeparatorChar, '/');

                MatchCollection matches = Regex.Matches(line, @"('([^']*)'|""([^""]*)"")");
                foreach (Match match in matches)
                {
                    string path = match.Groups[1].Value.Trim().Trim(new char[] { '\'', '\"' }).Replace(Path.DirectorySeparatorChar, '/');
                    if (!string.IsNullOrEmpty(path))
                    {
                        lists.include_directories.Add(path);
                    }
                }

                char[] separators = { ' ' };
                string[] items = Regex.Replace(line, @"('([^']*)'|""([^""]*)"")", "").Split(separators);
                foreach (string item in items)
                {
                    string path = item.Trim().Replace(Path.DirectorySeparatorChar, '/');
                    if (!string.IsNullOrEmpty(path))
                    {
                        lists.include_directories.Add(path);
                    }
                }

                include_directories.Command(command, state);
            }

            private CMakeListsParser lists;
            private CMakeParser.Core.IncludeDirectories include_directories;
        }

        public List<string> include_directories = new List<string>();
    }

    public class CppProperties
    {
        public string environment;
        public List<string> directory;
        public string header_cache;
        public string file;

        public void Save()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string path = Global.dte2.Solution.FullName;
            IVsTaskStatusCenterService status_center = VSHelper.GetVsTaskStatusCenterService();

            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                //https://learn.microsoft.com/zh-cn/visualstudio/extensibility/ux-guidelines/notifications-and-progress-for-visual-studio?view=vs-2019
                //https://learn.microsoft.com/zh-cn/visualstudio/extensibility/vsix/recipes/show-progress?view=vs-2019
                TaskHandlerOptions options = default;
                options.Title = "MAKE";
                options.ActionsAfterCompletion = CompletionActions.None;
                options.DisplayTaskDetails = delegate {};
                TaskProgressData data = default;
                data.CanBeCanceled = false;
                data.ProgressText = "开始扫描CMakeLists.txt文件";

                ITaskHandler handler = status_center.PreRegister(options, data);
                System.Threading.Tasks.Task task = null;

                switch (this.environment)
                {
                    case "Win32":
                        task = VSHelper.WorkThreadRunAsync(async delegate
                        {
                            await SaveWin32(path, this.environment);
                        });
                        break;
                    case "Win64":
                        task = VSHelper.WorkThreadRunAsync(async delegate
                        {
                            await SaveWin64(path, this.environment);
                        });
                        break;
                    case "Linux":
                        task = VSHelper.WorkThreadRunAsync(async delegate
                        {
                            await SaveLinux(path, this.environment);
                        });
                        break;
                    default:
                        VSHelper.ShowMessageBox("", "不支持的环境", OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGICON.OLEMSGICON_WARNING);
                        break;
                }

                handler.RegisterTask(task);
            });
        }

        private async Task SaveWin32(string path, string name)
        {
            JObject configuration = new JObject();
            configuration.Add("name", name);
            JArray inheritEnvironments = new JArray();
            inheritEnvironments.Add("msvc_x86");
            configuration.Add("inheritEnvironments", inheritEnvironments);
            JArray includePath = new JArray();
            //
            List<string> include_directorys = await GetCMakeListsIncludeDirectoryAsync(path, null);
            foreach (string directory in include_directorys)
            {
                includePath.Add(directory);
            }
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\Whole Tomato\\Visual Assist X\\VANet16\\Custom", "AdditionalInclude", "");
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\Whole Tomato\\Visual Assist X\\VANet16\\Custom", "SystemInclude", "");
            //
            foreach (string directory in this.directory)
            {
                includePath.Add(directory);
            }
            includePath.Add("${env.INCLUDE}");
            configuration.Add("includePath", includePath);
            configuration.Add("intelliSenseMode", "windows-msvc-x86");

            JObject root = new JObject();
            JArray configurations = new JArray();
            configurations.Add(configuration);
            root.Add("configurations", configurations);
            VSHelper.SaveJson(root, this.file);
        }

        private async Task SaveWin64(string path, string name)
        {
            JObject configuration = new JObject();
            configuration.Add("name", name);
            JArray inheritEnvironments = new JArray();
            inheritEnvironments.Add("msvc_x64");
            configuration.Add("inheritEnvironments", inheritEnvironments);
            JArray includePath = new JArray();
            //
            List<string> include_directorys = await GetCMakeListsIncludeDirectoryAsync(path, null);
            foreach (string directory in include_directorys)
            {
                includePath.Add(directory);
            }
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\Whole Tomato\\Visual Assist X\\VANet16\\Custom", "AdditionalInclude", "");
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\Whole Tomato\\Visual Assist X\\VANet16\\Custom", "SystemInclude", "");
            //
            foreach (string directory in this.directory)
            {
                includePath.Add(directory);
            }
            includePath.Add("${env.INCLUDE}");
            configuration.Add("includePath", includePath);
            configuration.Add("intelliSenseMode", "windows-msvc-x64");

            JObject root = new JObject();
            JArray configurations = new JArray();
            configurations.Add(configuration);
            root.Add("configurations", configurations);

            VSHelper.SaveJson(root, this.file);
        }

        private async Task SaveLinux(string path, string name)
        {
            JObject configuration = new JObject();
            configuration.Add("name", name);
            JArray inheritEnvironments = new JArray();
            inheritEnvironments.Add("linux_x64");
            configuration.Add("inheritEnvironments", inheritEnvironments);

            JArray defines = new JArray();
            JArray includePath = new JArray();
            if (this.header_cache != null)
            {
                string filename = Path.Combine(this.header_cache, "cachedCompilerDetails.json");
                if (File.Exists(filename))
                {
                    //List<string> defs = GetLinuxDefines(filename);
                    //foreach(string def in defs)
                    //{
                    //    defines.Add(def);
                    //}
                    List<string> directorys = GetLinuxIncludeDirectory(filename);
                    foreach (string dir in directorys)
                    {
                        includePath.Add(dir);
                    }
                    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Whole Tomato\\Visual Assist X\\VANet16\\Custom", "AdditionalInclude", string.Join(";", directorys));
                    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Whole Tomato\\Visual Assist X\\VANet16\\Custom", "SystemInclude", string.Join(";", directorys));
                }
            }
            List<string> include_directorys = await GetCMakeListsIncludeDirectoryAsync(path, this.header_cache);
            foreach (string directory in include_directorys)
            {
                includePath.Add(directory);
            }
            foreach(string directory in this.directory)
            {
                if (!string.IsNullOrEmpty(directory) && directory[0] == '/')
                {
                    if (!string.IsNullOrEmpty(this.header_cache))
                    {
                        includePath.Add(this.header_cache + directory);
                    }
                }
                else
                {
                    includePath.Add(directory);
                }
            }
            //includePath.Add("${env.INCLUDE}");
            configuration.Add("defines", defines);
            configuration.Add("includePath", includePath);
            configuration.Add("intelliSenseMode", "linux-gcc-x64");

            JObject root = new JObject();
            JArray configurations = new JArray();
            configurations.Add(configuration);
            root.Add("configurations", configurations);

            VSHelper.SaveJson(root, this.file);
        }

        private static async Task<List<string>> GetCMakeListsIncludeDirectoryAsync(string solution_path, string sys_root)
        {
            List<string> entries = new List<string>();
            string[] files = Directory.GetFiles(solution_path, "CMakeLists.txt", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                CMakeListsParser lists = new CMakeListsParser(file);
                foreach (string directory in lists.include_directories)
                {
                    if (!string.IsNullOrEmpty(directory) && directory[0] == '/')
                    {
                        if (!string.IsNullOrEmpty(sys_root))
                        {
                            entries.Add(sys_root + directory);
                        }
                    }
                    else
                    {
                        entries.Add(directory);
                    }
                }
            }
            entries.Sort();
            return entries.Distinct().ToList();
        }

        private static List<string> GetLinuxDefines(string filename)
        {
            List<string> defines = new List<string>();
            string content = File.ReadAllText(filename);
            JObject cachedCompilerDetails = JObject.Parse(content);
            if (cachedCompilerDetails["compilers"] is JArray compilers)
            {
                foreach (JObject compiler in compilers)
                {
                    if (compiler["defines"] is JObject defs)
                    {
                        foreach (var key_value in defs)
                        {
                            string def = string.Format("{0}={1}", key_value.Key, key_value.Value.ToString());
                            defines.Add(def);
                        }
                    }
                }
            }
            defines.Sort();
            return defines.Distinct().ToList();
        }

        private static List<string> GetLinuxIncludeDirectory(string filename)
        {
            List<string> directorys = new List<string>();
            string directory = Path.GetDirectoryName(filename).Replace(Path.DirectorySeparatorChar, '/');
            string content = File.ReadAllText(filename);
            JObject cachedCompilerDetails = JObject.Parse(content);
            if (cachedCompilerDetails["compilers"] is JArray compilers)
            {
                foreach (JObject compiler in compilers)
                {
                    if (compiler["includes"] is JArray includes)
                    {
                        foreach (var include in includes)
                        {
                            directorys.Add(directory + include.ToString().Replace(Path.DirectorySeparatorChar, '/'));
                        }
                    }
                }
            }
            directorys.Sort();
            return directorys.Distinct().ToList();
        }
    }
}
