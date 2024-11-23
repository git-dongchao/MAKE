using System;
using System.IO;
using Microsoft.MIDebugEngine;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MAKE
{
    class DebugProject
    {
        public static void Start(string file, string args, string envs)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var extension = Path.GetExtension(file);
            if (extension == ".exe")
            {
                VsDebugTargetInfo target_info = new VsDebugTargetInfo();
                target_info.bstrExe = file;
                target_info.bstrCurDir = System.IO.Path.GetDirectoryName(file);
                target_info.bstrArg = args;
                target_info.bstrEnv = envs;

                IVsDebugger debugger = VSHelper.StartDebug(target_info);
                if (debugger != null)
                {
                    if (debugger_events_cookie != 0)
                    {
                        debugger.UnadviseDebuggerEvents(debugger_events_cookie);
                    }
                    debugger.AdviseDebuggerEvents(new DebuggerEventManager(), out debugger_events_cookie);
                }
            }
            else if (extension == "")
            {
                SSHLaunchOptions launch = Global.config.CreateSSHLaunchOptions(file, args, envs);
                if (launch.SaveXml())
                {
                    try
                    {
                        Global.dte2.ExecuteCommand("Debug.MIDebugLaunch", $"/Executable:SSH /OptionsFile:\"{launch.XmlFile}\"");

                        IVsDebugger debugger = VSHelper.GetVsDebugger();
                        if (debugger != null)
                        {
                            if (debugger_events_cookie != 0)
                            {
                                debugger.UnadviseDebuggerEvents(debugger_events_cookie);
                            }
                            debugger.AdviseDebuggerEvents(new DebuggerEventManager(), out debugger_events_cookie);
                        }
                    }
                    catch (Exception ex)
                    {
                        IVsCommandWindow commandWindow = Package.GetGlobalService(typeof(SVsCommandWindow)) as IVsCommandWindow;
                        commandWindow.PrintNoShow($"Error: {ex.Message}\r\n");
                        return;
                    }

                    Guid guid = Guid.NewGuid();
                    string tty_file = $"/tmp/tty_{guid}";
                    string working_directory = Global.config.GetLinuxDirectory(file);
                    TerminalManager.CreateSSHByCommand(working_directory, $"tty > {tty_file} ; tail -f /dev/null");
                    WaitSshTtyCompleted(tty_file);
                }
            }
            else
            {
                VSHelper.ShowMessageBox("", "不支持的文件类型", OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGICON.OLEMSGICON_WARNING);
            }
        }

        public static bool IsDebugger()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return (Global.dte2 != null && Global.dte2.Debugger != null && Global.dte2.Debugger.CurrentMode != EnvDTE.dbgDebugMode.dbgDesignMode);
        }

        public static bool IsEnable(string file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (File.Exists(file))
            {
                var extension = Path.GetExtension(file);
                if (extension == ".exe" || extension == "")
                {
                    return true;
                }
            }
            return false;
        }

        private static async void WaitSshTtyCompleted(string file, int count = 0)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                string result;
                string command = $"shell if [ -f '{file}' ]; then echo 'true' ; else echo 'false'; fi";
                result = await MIDebugCommandDispatcher.ExecuteCommand(command);
                if (result.Trim() == "true")
                {
                    command = $"shell if [ -f '{file}' ]; then cat '{file}' ; fi";
                    string tty = await MIDebugCommandDispatcher.ExecuteCommand(command);
                    tty = tty.Trim();
                    if (tty.Length > 0)
                    {
                        command = $"set inferior-tty {tty}";
                        result = await MIDebugCommandDispatcher.ExecuteCommand(command);
                        result = await MIDebugCommandDispatcher.ExecuteCommand("run");
                    }
                }
                else if (result.Trim() == "false")
                {
                    if (count < 50)
                    {
                        await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(100));
                        WaitSshTtyCompleted(file, ++count);
                    }
                    else
                    {
                        IVsOutputWindowPane pane = VSHelper.GetDebugOutputWindow();
                        if (pane != null)
                        {
                            pane.OutputString($"Error: the '{file}' file does not exist\r\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                IVsCommandWindow commandWindow = Package.GetGlobalService(typeof(SVsCommandWindow)) as IVsCommandWindow;
                commandWindow.PrintNoShow($"Error: {ex.Message}\r\n");
            }
        }

        public static uint debugger_events_cookie = 0;
    }
}
