using System;
using System.IO;
using System.Text.RegularExpressions;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace MAKE
{
    class Global
    {
        public static DTE2 dte2;
        public static FileChangeListener file_change_listener = null;
        public static Config config;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            // DTE2
            dte2 = await package.GetServiceAsync(typeof(SDTE)).ConfigureAwait(false) as DTE2;
            
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            
            file_change_listener = new FileChangeListener();
            
            file_change_listener.OnFileChange += WindowEventManger.OnFileChange;
            
            config = new Config();
            if (VSHelper.IsPackageEnable(dte2))
            {
                WindowEventManger.OnAfterOpenFolder(null, null);
            }

            Microsoft.VisualStudio.Shell.Events.SolutionEvents.OnAfterOpenFolder += WindowEventManger.OnAfterOpenFolder;
            Microsoft.VisualStudio.Shell.Events.SolutionEvents.OnAfterOpenSolution += WindowEventManger.OnAfterOpenSolution;
            Microsoft.VisualStudio.Shell.Events.SolutionEvents.OnBeforeCloseSolution += WindowEventManger.OnBeforeCloseSolution;
        }

        public static string WindowsEnvironment(string envs = "")
        {
            string VSAPPIDDIR = System.Environment.GetEnvironmentVariable("VSAPPIDDIR");
            string command = $"set VCVARSALL={VSAPPIDDIR}/../../VC/Auxiliary/Build/vcvarsall.bat";
            command += " & set VS2015=-vcvars_ver=14.0 & set VS2017=-vcvars_ver=14.1 & set VS2019=-vcvars_ver=14.2 & set VS2022=-vcvars_ver=14.3";
            MatchCollection matches = Regex.Matches(envs, @"(?:(\w+)=(?:(?:""([^""]*)"")|(?:'([^']*)')|([^ ]*)))");
            foreach(Match match in matches)
            {
                command += $" & set {match.Groups[1].Value}={match.Groups[2].Value}{match.Groups[3].Value}{match.Groups[4].Value}";
            }
            return command;
        }

        public static string LinuxEnvironment(string envs = "")
        {
            string command = "export";
            string ld_library_path = "./";
            MatchCollection matches = Regex.Matches(envs, @"(?:(\w+)=(?:(?:""([^""]*)"")|(?:'([^']*)')|([^ ]*)))");
            foreach (Match match in matches)
            {
                if (match.Groups[1].Value == "LD_LIBRARY_PATH")
                {
                    ld_library_path = $"./:{match.Groups[2].Value}{match.Groups[3].Value}{match.Groups[4].Value}";
                }
                else
                {
                    command += $" {match.Groups[1].Value}=\"{match.Groups[2].Value}{match.Groups[3].Value}{match.Groups[4].Value}\"";
                }
            }
            return command + $" LD_LIBRARY_PATH=\"{ld_library_path}\"";
        }
    }

    class WindowEventManger
    {
        public static void OnAfterOpenFolder(Object sender, FolderEventArgs e)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                Global.config.Reset();
                Global.config.Load();
                Global.config.CreateCppProperties().Save();
                Global.file_change_listener.Subscribe(Global.config.GetPrivatePath("Project.json"));
                VSHelper.ShowTaskMessage("加载目录成功", true, true);
            }
            catch (Exception ex)
            {
                VSHelper.ShowTaskMessage(ex.Message, false, true);
            }
        }
        public static void OnAfterOpenSolution(object sender, EventArgs e)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                Global.config.Reset();
                Global.config.Load();
                Global.config.CreateCppProperties().Save();
                Global.file_change_listener.Subscribe(Global.config.GetPrivatePath("Project.json"));
                VSHelper.ShowTaskMessage("加载目录成功", true, true);
            }
            catch (Exception ex)
            {
                VSHelper.ShowTaskMessage(ex.Message, false, true);
            }
        }
        public static void OnBeforeCloseSolution(object sender, EventArgs e)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                Global.file_change_listener.Unsubscribe(Global.config.GetPrivatePath("Project.json"));
                Global.config.Save();
                TerminalManager.CloseAll();
            }
            catch (Exception ex)
            {
                VSHelper.ShowTaskMessage(ex.Message, false, true);
            }
        }
        public static void OnFileChange(object sender, FileEventArgs e)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                Global.config.Reset();
                Global.config.Load();
                Global.config.CreateCppProperties().Save();
            }
            catch (Exception ex)
            {
                VSHelper.ShowTaskMessage(ex.Message, false, true);
            }
        }
    }

    class DebuggerEventManager : IVsDebuggerEvents
    {
        public int OnModeChange(DBGMODE dbgmodeNew)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dbgmodeNew == DBGMODE.DBGMODE_Design)
            {
                TerminalManager.CloseDebug();
            }
            return VSConstants.S_OK;
        }
    }
}
