using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.Shell;
using liblinux.Persistence;
using System.Text.RegularExpressions;

namespace MAKE
{
    public class Config
    {
        public class TargetItem
        {
            public string target = "";
            public string args = "";
            public string envs = "";
        }

        public string target_machine;                           // 目标主机
        public string target_working_directory;                 // 目标工作目录
        public string terminal_type;                            // 终端类型
        // 生成项目
        public SortedDictionary<string, string> build_projects = new SortedDictionary<string, string>();
        // 运行/调试项目
        public SortedDictionary<string, TargetItem> targets = new SortedDictionary<string, TargetItem>();
        // IntelliSense环境
        public string intellisense_environment;
        // IntelliSense目录
        public SortedDictionary<string, List<String>> intellisense_directory = new SortedDictionary<string, List<String>>();

        public Config()
        {
        }

        public static string GetDirectoryName(string path)
        {
            return File.Exists(path) ? Path.GetDirectoryName(path) : path;
        }

        public static string GetFileName(string path)
        {
            return File.Exists(path) ? Path.GetFileName(path) : null;
        }

        public string GetLinuxDirectory(string filepath)
        {
            string directory;
            if (File.Exists(filepath))
            {
                directory = Path.GetDirectoryName(filepath);
            }
            else
            {
                directory = filepath;
            }
            string relative_path = directory.Replace(Global.dte2.Solution.FullName, "");
            string path = GetTargetWorkingDirectory() + relative_path;
            return path.Replace('\\', '/');
        }

        public string GetBuildProject(string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.build_projects.ContainsKey(name))
            {
                return Config.GetAbsolutePath(this.build_projects[name]);
            }
            return null;
        }

        public bool IsVSTerminal()
        {
            return this.terminal_type == "VisualStudio";
        }
        public bool IsConEmuTerminal()
        {
            return this.terminal_type == "ConEmu";
        }

        public void AddBuildProject(string name, string relative_target)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.build_projects[relative_target] = relative_target;
        }

        public TargetItem GetDebugProject(string relative_target)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (this.targets.ContainsKey(relative_target))
            {
                return new TargetItem
                {
                    target = Config.GetAbsolutePath(relative_target),
                    args = this.targets[relative_target].args,
                    envs = this.targets[relative_target].envs
                };
            }
            return null;
        }

        public TargetItem AddDebugProject(string relative_target)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!this.targets.ContainsKey(relative_target))
            {
                this.targets[relative_target] = new TargetItem();
            }
            return new TargetItem 
            {
                target = Config.GetAbsolutePath(relative_target), 
                args = this.targets[relative_target].args, 
                envs = this.targets[relative_target].envs 
            };
        }

        public TargetItem GetRunProject(string relative_target)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (this.targets.ContainsKey(relative_target))
            {
                return new TargetItem
                {
                    target = Config.GetAbsolutePath(relative_target),
                    args = this.targets[relative_target].args,
                    envs = this.targets[relative_target].envs
                };
            }
            return null;
        }

        public TargetItem AddRunProject(string relative_target)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!this.targets.ContainsKey(relative_target))
            {
                this.targets[relative_target] = new TargetItem();
            }
            return new TargetItem 
            {
                target = Config.GetAbsolutePath(relative_target),
                args = this.targets[relative_target].args, 
                envs = this.targets[relative_target].envs 
            };
        }

        public void Reset()
        {
            this.target_machine = "";
            this.target_working_directory = "";
            this.terminal_type = "VisualStudio";
            this.build_projects.Clear();
            this.targets.Clear();
            this.intellisense_environment = "Linux";
            this.intellisense_directory.Clear();
            this.intellisense_directory["Win32"] = new List<string>();
            this.intellisense_directory["Win64"] = new List<string>();
            this.intellisense_directory["Linux"] = new List<string>();
        }

        public void Load()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string file = this.GetPrivatePath("Project.json");
            if (!File.Exists(file))
            {
                return;
            }

            string content = File.ReadAllText(file);
            JObject root = JObject.Parse(content);
            if (root["target_machine"] != null)
            {
                this.target_machine = root["target_machine"].ToString();
            }
            if (root["target_working_directory"] != null)
            {
                this.target_working_directory = root["target_working_directory"].ToString();
            }
            if (root["terminal_type"] != null)
            {
                this.terminal_type = root["terminal_type"].ToString();
            }
            if (this.terminal_type != "VisualStudio" && this.terminal_type != "ConEmu")
            {
                this.terminal_type = "VisualStudio";
            }

            var build_projects = (JObject)root["build_projects"];
            this.build_projects.Clear();
            if (build_projects != null)
            {
                foreach (var key_val in build_projects)
                {
                    this.build_projects.Add(key_val.Key, key_val.Value.ToString());
                }
            }

            var targets = (JObject)root["targets"];
            this.targets.Clear();
            if (targets != null)
            {
                foreach (var key_val in targets)
                {
                    var json = (JObject)key_val.Value;
                    TargetItem item = new TargetItem();
                    if (json["args"] != null)
                    {
                        item.args = json["args"].ToString();
                    }
                    if (json["envs"] != null && !string.IsNullOrEmpty(json["envs"].ToString()))
                    {
                        item.envs = json["envs"].ToString();
                    }
                    this.targets.Add(key_val.Key, item);
                }
            }

            var intellisense_environment = (JValue)root["intellisense_environment"];
            if (intellisense_environment != null)
            {
                this.intellisense_environment = intellisense_environment.ToString();
            }
            else
            {
                this.intellisense_environment = "Linux";
            }

            var intellisense_directory = (JObject)root["intellisense_directory"];
            this.intellisense_directory.Clear();
            if (intellisense_directory != null)
            {
                foreach (var key_val in intellisense_directory)
                {
                    if (!this.intellisense_directory.ContainsKey(key_val.Key))
                    {
                        this.intellisense_directory[key_val.Key] = new List<string>();
                    }
                    var paths = (JArray)key_val.Value;
                    foreach (JValue path in paths)
                    {
                        this.intellisense_directory[key_val.Key].Add(path.ToString());
                    }
                }
            }
        }

        public string Save()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            JObject root = new JObject();
            root.Add("target_machine", this.target_machine);
            root.Add("target_working_directory", this.target_working_directory);
            root.Add("terminal_type", this.terminal_type);

            JObject build_projects = new JObject();
            foreach (var key_val in this.build_projects)
            {
                build_projects.Add(key_val.Key, key_val.Value);
            }
            JObject targets = new JObject();
            foreach (var key_val in this.targets)
            {
                var target = new JObject();
                target.Add("args", key_val.Value.args);
                target.Add("envs", key_val.Value.envs);
                targets.Add(key_val.Key, target);
            }

            JObject intellisense_directory = new JObject();
            foreach (var key_val in this.intellisense_directory)
            {
                JArray array = new JArray();
                foreach (string path in key_val.Value)
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        array.Add(path);
                    }
                }
                intellisense_directory.Add(key_val.Key, array);
            }

            root.Add("build_projects", build_projects);
            root.Add("targets", targets);
            root.Add("intellisense_environment", this.intellisense_environment);
            root.Add("intellisense_directory", intellisense_directory);

            string file = this.GetPrivatePath("Project.json");
            using (FileStream fs = File.Open(file, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs, new System.Text.UTF8Encoding()))
                {
                    using (JsonTextWriter jw = new JsonTextWriter(sw))
                    {
                        jw.Formatting = Newtonsoft.Json.Formatting.Indented;
                        jw.Indentation = ' ';
                        jw.Indentation = 4;
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(jw, root);
                    }
                }
            }
            return file;
        }

        public CppProperties CreateCppProperties()
        {
            string cache_file = null;
            StoredConnectionInfo connection = SSHLaunchOptions.GetConnectionInfo(this.target_machine);
            if (connection != null)
            {
                if (connection.Properties.Get("Sysroot") != null)
                {
                    cache_file = connection.Properties["Sysroot"].Replace(Path.DirectorySeparatorChar, '/');
                }
            }

            return new CppProperties
            {
                environment = this.intellisense_environment,
                directory = this.intellisense_directory[this.intellisense_environment],
                header_cache = cache_file,
                file = Path.Combine(Global.dte2.Solution.FullName, "CppProperties.json")
            };
        }

        public SSHLaunchOptions CreateSSHLaunchOptions(string file = "", string args = "", string envs = "")
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            SSHLaunchOptions launch = new SSHLaunchOptions
            {
                XmlFile = this.GetPrivatePath("SSHLaunchOptions.xml"),
                TargetMachine = this.target_machine,
                WorkingDirectory = this.GetLinuxDirectory(file),
                ExePath = Path.GetFileName(file),
                ExeArguments = args
            };

            MatchCollection matches = Regex.Matches(envs, @"(?:(\w+)=(?:(?:'([^']*)')|([^ ]*)))");
            foreach (Match match in matches)
            {
                launch.Environment.Add(new SSHLaunchOptions.EnvironmentInfo { Name = match.Groups[1].Value, Value = match.Groups[2].Value + match.Groups[3].Value });
            }
            return launch;
        }

        private string GetTargetWorkingDirectory()
        {
            char[] trim = { '\\', '/' };
            return target_working_directory.TrimEnd(trim);
        }

        public string GetPrivatePath(string filename)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string path = Global.dte2.Solution.FullName;
            if (System.IO.File.Exists(path))
            {
                return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), ".vs", filename);
            }
            else if (System.IO.Directory.Exists(path))
            {
                return System.IO.Path.Combine(path, ".vs", filename);
            }
            return "";
        }

        public static string GetRelativePath(string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string root_path = Global.dte2.Solution.FullName.Replace('\\', '/');
            return path.Replace('\\', '/').Replace(root_path, "").TrimStart("/".ToCharArray());
        }
        
        private static string GetAbsolutePath(string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return Path.Combine(Global.dte2.Solution.FileName, path).Replace('\\', '/');
        }
    }
}
