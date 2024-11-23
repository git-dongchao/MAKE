using System;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using EnvDTE80;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TaskStatusCenter;
using Package = Microsoft.VisualStudio.Shell.Package;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MAKE
{
    class VSHelper
    {
        public static bool IsPackageEnable(DTE2 dte2)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return (dte2 != null && dte2.Solution != null && dte2.Solution.IsOpen && dte2.Solution.FullName.Length > 0);
        }

        public static string GetExeDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
        public static string GetConEmuExePath()
        {
            return Path.Combine(GetExeDirectory(), "ConEmu", "ConEmu.exe");
        }
        public static string GetSshExePath()
        {
            return Path.Combine(GetExeDirectory(), "ssh", "ssh.exe");
        }

        public static IVsSolutionBuildManager2 GetSolutionBuildManager()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution_manager == null)
            {
                solution_manager = Package.GetGlobalService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;
            }
            return solution_manager;
        }

        public static IVsMonitorSelection GetMonitorSelection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (monitor_selection == null)
            {
                monitor_selection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            }
            return monitor_selection;
        }

        public static string GetCurrentSelection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (monitor_selection == null)
            {
                monitor_selection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            }

            int Result = VSConstants.S_OK;
            IntPtr HierarchyPtr;
            IntPtr SelectionContainerPointer;
            uint ProjectItemId;
            IVsMultiItemSelect MultiItemSelect;
            Result = monitor_selection.GetCurrentSelection(out HierarchyPtr, out ProjectItemId, out MultiItemSelect, out SelectionContainerPointer);
            if (Result == VSConstants.S_OK && HierarchyPtr != IntPtr.Zero)
            {
                // single select
                if (MultiItemSelect == null)
                {
                    IVsHierarchy Hierarchy = Marshal.GetTypedObjectForIUnknown(HierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
                    string path;
                    Result = Hierarchy.GetCanonicalName(ProjectItemId, out path);
                    return (Result == VSConstants.S_OK ? path : null);
                }
            }
            return null;
        }

        public static IVsFileChangeEx GetFileChangeEx()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return Package.GetGlobalService(typeof(SVsFileChangeEx)) as IVsFileChangeEx;
        }

        public static IVsDebugger GetVsDebugger()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return Package.GetGlobalService(typeof(SVsShellDebugger)) as IVsDebugger;
        }

        public static IVsStatusbar GetVsStatusBar()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
        }

        public static IVsTaskStatusCenterService GetVsTaskStatusCenterService()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return Package.GetGlobalService(typeof(SVsTaskStatusCenterService)) as IVsTaskStatusCenterService;
        }

        public static IVsOutputWindowPane GetDebugOutputWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsOutputWindow output_window = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            output_window.GetPane(VSConstants.OutputWindowPaneGuid.DebugPane_guid, out IVsOutputWindowPane pane);
            return pane;
        }

        public static IVsPackage GetTerminalWindowPackage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsPackage vs_package = null;
            IVsShell vs_shell = Package.GetGlobalService(typeof(SVsShell)) as IVsShell;
            Guid terminal_package_guid = new Guid("E632CA91-B170-401A-AC2E-6B83FFDC3C10");
            if (vs_shell.IsPackageLoaded(terminal_package_guid, out vs_package) == VSConstants.S_OK)
            {
                return vs_package;
            }
            if (vs_shell.LoadPackage(terminal_package_guid, out vs_package) == VSConstants.S_OK)
            {
                return vs_package;
            }
            return null;
        }

        public static IVsDebugger StartDebug(VsDebugTargetInfo target_info)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsDebugger4 debugger = Package.GetGlobalService(typeof(IVsDebugger)) as IVsDebugger4;
            VsDebugTargetInfo4[] debug_targets = new VsDebugTargetInfo4[1];
            debug_targets[0].dlo = (uint)DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;
            debug_targets[0].bstrExe = target_info.bstrExe;
            debug_targets[0].bstrCurDir = target_info.bstrCurDir;
            debug_targets[0].bstrArg = target_info.bstrArg;
            debug_targets[0].guidLaunchDebugEngine = VSConstants.DebugEnginesGuids.NativeOnly_guid;

            debug_targets[0].bstrEnv = "";
            MatchCollection matches = Regex.Matches(target_info.bstrEnv, @"(?:(\w+)=(?:(?:""([^""]*)"")|(?:'([^']*)')|([^ ]*)))");
            foreach (Match match in matches)
            {
                debug_targets[0].bstrEnv += $"{match.Groups[1].Value}={match.Groups[2].Value}{match.Groups[3].Value}{match.Groups[4].Value}\0";
            }
            if (string.IsNullOrEmpty(debug_targets[0].bstrEnv))
            {
                debug_targets[0].bstrEnv = null;
            }
            debugger.LaunchDebugTargets4(1, debug_targets, null);
            return debugger as IVsDebugger;
        }

        public static void ReloadSolution(DTE2 dte2)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string filename = dte2.Solution.FullName;
            IVsSolution solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            solution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_UnloadProject, null, 0);
            solution.OpenSolutionFile(0, filename);
        }

        public static void Restart()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsShell4 vs_shell = Package.GetGlobalService(typeof(SVsShell)) as IVsShell4;
            vs_shell.Restart((uint)__VSRESTARTTYPE.RESTART_Normal);
        }

        public static void SaveJson(JObject root, string file)
        {
            if (root != null)
            {
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
            }
        }

        public static async Task MainThreadRunAsync(Func<Task> async_method)
        {
            await ThreadHelper.JoinableTaskFactory.RunAsync(async_method);
        }
        public static async Task<T> MainThreadRunAsync<T>(Func<Task<T>> async_method)
        {
            return await ThreadHelper.JoinableTaskFactory.RunAsync(async_method);
        }

        public static async Task WorkThreadRunAsync(Func<Task> async_method)
        {
            await Task.Run(async_method);
        }
        public static async Task<T> WorkThreadRunAsync<T>(Func<Task<T>> async_method)
        {
            return await Task.Run(async_method);
        }

        public static void ShowTaskMessage(string message, bool is_success, bool is_notify)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                IVsTaskStatusCenterService status_center = VSHelper.GetVsTaskStatusCenterService();
                TaskHandlerOptions options = default;
                options.Title = "MAKE";
                options.ActionsAfterCompletion = CompletionActions.None;
                options.DisplayTaskDetails = delegate { };
                if (is_notify)
                {
                    options.ActionsAfterCompletion = is_success ? CompletionActions.RetainAndNotifyOnRanToCompletion : CompletionActions.RetainAndNotifyOnFaulted;
                }
                else
                {
                    options.ActionsAfterCompletion = is_success ? CompletionActions.RetainOnRanToCompletion : CompletionActions.RetainOnFaulted;
                }
                TaskProgressData data = default;
                data.CanBeCanceled = false;
                data.ProgressText = message;
                status_center.PreRegister(options, data);
            });
        }

        public static int ShowMessageBox(string title, string message, OLEMSGBUTTON msgbtn = OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGICON msgicon = OLEMSGICON.OLEMSGICON_INFO)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsUIShell vs_shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            if (vs_shell != null)
            {
                int result;
                Guid clsid = Guid.Empty;
                vs_shell.ShowMessageBox(0, ref clsid, title, message, string.Empty, 0,
                    msgbtn, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, msgicon,
                    0, out result);
                return result;
            }
            return 0;
        }

        private static IVsMonitorSelection monitor_selection = null;
        private static IVsSolutionBuildManager2 solution_manager = null;
    }

}