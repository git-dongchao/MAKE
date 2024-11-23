using System;
using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;
using liblinux;
using liblinux.Persistence;
using Microsoft.VisualStudio.Shell.Interop;

namespace MAKE
{
    public class SSHLaunchOptions
    {
        public static List<string> GetConnectionList()
        {
            List<string> infos = new List<string>();
            ConnectionInfoStore store = new ConnectionInfoStore();
            foreach (StoredConnectionInfo connection in store.Connections)
            {
                infos.Add($"{connection.ConnectionInfo.UserName}@{connection.ConnectionInfo.HostNameOrAddress}:{connection.ConnectionInfo.Port}");
            }
            infos.Sort();
            return infos;
        }

        public static StoredConnectionInfo GetConnectionInfo(string TargetMachine)
        {
            ConnectionInfoStore store = new ConnectionInfoStore();
            foreach (StoredConnectionInfo connection in store.Connections)
            {
                ConnectionInfo info = connection.ConnectionInfo;
                if ($"{info.UserName}@{info.HostNameOrAddress}:{info.Port}" == TargetMachine)
                {
                    return connection;
                }
            }
            return null;
        }

        public SSHLaunchOptions()
        {
            Environment = new List<EnvironmentInfo>();
        }

        public string GetUser()
        {
            Match match = Regex.Match(this.TargetMachine, @"^(?<user>.+?)@(?<host>.+?):(?<port>[0-9]+?)$");
            if (match.Success)
            {
                return match.Groups["user"].Value;
            }
            return null;
        }

        public string GetPassword()
        {
            ConnectionInfo info = GetConnectionInfo(this.TargetMachine);
            if (info != null && info is PasswordConnectionInfo pwd_info)
            {
                IntPtr ptr = IntPtr.Zero;
                string password;
                try
                {
                    ptr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocAnsi(pwd_info.Password);
                    password = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.ZeroFreeCoTaskMemAnsi(ptr);
                }
                return password;
            }
            return null;
        }

        public string GetHost()
        {
            Match match = Regex.Match(this.TargetMachine, @"^(?<user>.+?)@(?<host>.+?):(?<port>[0-9]+?)$");
            if (match.Success)
            {
                return match.Groups["host"].Value;
            }
            return null;
        }

        public int GetPort()
        {
            Match match = Regex.Match(this.TargetMachine, @"^(?<user>.+?)@(?<host>.+?):(?<port>[0-9]+?)$");
            if (match.Success)
            {
                return int.Parse(match.Groups["port"].Value);
            }
            return 22;
        }

        public bool LoadXml()
        {
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(this.XmlFile);
                // SSHLaunchOptions
                var root = xml.SelectSingleNode("SSHLaunchOptions");
                // TargetMachine
                var TargetMachine = ((XmlElement)root).GetAttribute("TargetMachine");
                Match match = Regex.Match(TargetMachine, @"^(?<user>.+?)@(?<host>.+?):(?<port>[0-9]+?)$");
                if (!match.Success)
                {
                    match = Regex.Match(TargetMachine, @"^(?<user>.+?)@(?<host>.+?)$");
                    if (match.Success)
                    {
                        TargetMachine += ":22";
                    }
                }
                // WorkingDirectory
                WorkingDirectory = ((XmlElement)root).GetAttribute("WorkingDirectory");
                // ExePath
                ExePath = ((XmlElement)root).GetAttribute("ExePath");
                // ExeArguments
                ExeArguments = ((XmlElement)root).GetAttribute("ExeArguments");
                // Environment
                var Environment = root.SelectSingleNode("Environment");
                this.Environment.Clear();
                if (Environment != null)
                {
                    foreach (XmlNode node in Environment.ChildNodes)
                    {
                        EnvironmentInfo info = new EnvironmentInfo();
                        info.Name = ((XmlElement)node).GetAttribute("Name");
                        info.Value = ((XmlElement)node).GetAttribute("Value");
                        this.Environment.Add(info);
                    }
                }
            }
            catch (Exception ex)
            {
                VSHelper.ShowMessageBox("", ex.Message, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGICON.OLEMSGICON_WARNING);
                return false;
            }
            return true;
        }

        public bool SaveXml()
        {
            try
            {
                XmlDocument xml = new XmlDocument();
                var root = xml.CreateElement("SSHLaunchOptions");
                ///< TargetMachine
                root.SetAttribute("TargetMachine", this.GetTargetMachine());
                ///< TargetArchitecture
                StoredConnectionInfo connect = GetConnectionInfo(this.TargetMachine); ;
                string TargetArchitecture = connect?.Properties.Get("Platform");
                root.SetAttribute("TargetArchitecture", TargetArchitecture ?? "x64");
                ///< MIMode
                root.SetAttribute("MIMode", "gdb");
                ///< WorkingDirectory
                root.SetAttribute("WorkingDirectory", this.WorkingDirectory);
                ///< ExePath
                root.SetAttribute("ExePath", this.ExePath);
                ///< ExeArguments
                root.SetAttribute("ExeArguments", this.ExeArguments);
                root.SetAttribute("AdditionalSOLibSearchPath", WorkingDirectory);
                root.SetAttribute("StartRemoteDebuggerCommand", "gdb --interpreter=mi");
                ///< Environment
                var Environment = xml.CreateElement("Environment");
                
                foreach (EnvironmentInfo info in this.Environment)
                {
                    var EnvironmentEntry = xml.CreateElement("EnvironmentEntry");
                    EnvironmentEntry.SetAttribute("Name", info.Name);
                    EnvironmentEntry.SetAttribute("Value", info.Value);
                    Environment.AppendChild(EnvironmentEntry);
                }
                if (Environment.ChildNodes.Count > 0)
                {
                    root.AppendChild(Environment);
                }
                //LaunchCompleteCommand
                var LaunchCompleteCommand = xml.CreateElement("LaunchCompleteCommand");
                LaunchCompleteCommand.InnerText = "None";
                root.AppendChild(LaunchCompleteCommand);

                xml.AppendChild(root);
                xml.Save(this.XmlFile);
            }
            catch (Exception ex)
            {
                VSHelper.ShowMessageBox("", ex.Message, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGICON.OLEMSGICON_WARNING);
                return false;
            }
            return true;
        }

        private string GetTargetMachine()
        {
            Match match = Regex.Match(this.TargetMachine, @"^(?<user>.+?)@(?<host>.+?):(?<port>[0-9]+?)$");
            if (match.Success)
            {
                if (int.Parse(match.Groups["port"].Value) == 22)
                {
                    return $"{match.Groups["user"].Value}@{match.Groups["host"].Value}";
                }
                return this.TargetMachine;
            }
            return null;
        }

        public class EnvironmentInfo
        {
            public string Name;
            public string Value;
        }

        public string XmlFile;
        public string TargetMachine;
        public string WorkingDirectory;
        public string ExePath;
        public string ExeArguments;
        public List<EnvironmentInfo> Environment;
    }
}
