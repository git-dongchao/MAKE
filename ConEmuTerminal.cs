using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace MAKE
{
    public class ConEmuTerminal
    {
        public static ConEmuTerminal CreateSSHTerminal(string path, string args, string envs)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string file_name = Config.GetFileName(path);
            SSHLaunchOptions launch = Global.config.CreateSSHLaunchOptions();
            ///< arguments
            string arguments;
            if (file_name != null && file_name.Length > 0)
            {
                if (String.IsNullOrEmpty(args))
                {
                    arguments = string.Format("-StartTSA -NoCloseConfirm -run \"{0}\" -o StrictHostKeyChecking=no -Z {1} -p {2} -t -t {3}@{4} \"cd \"{5}\" ; clear ; {6} ; ./{7} ; bash\"",
                        VSHelper.GetSshExePath(),
                        launch.GetPassword(),
                        launch.GetPort(),
                        launch.GetUser(),
                        launch.GetHost(),
                        Global.config.GetLinuxDirectory(path),
                        envs,
                        file_name);
                }
                else
                {
                    arguments = string.Format("-StartTSA -NoCloseConfirm -run \"{0}\" -o StrictHostKeyChecking=no -Z {1} -p {2} -t -t {3}@{4} \"cd \"{5}\" ; clear ; {6} ; ./{7} {8} ; bash\"",
                        VSHelper.GetSshExePath(),
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
                arguments = string.Format("-StartTSA -NoCloseConfirm -run \"{0}\" -o StrictHostKeyChecking=no -Z {1} -p {2} -t -t {3}@{4} \"cd \"{5}\" ; {6} ; bash\"",
                    VSHelper.GetSshExePath(),
                    launch.GetPassword(),
                    launch.GetPort(),
                    launch.GetUser(),
                    launch.GetHost(),
                    Global.config.GetLinuxDirectory(path),
                    envs);
            }

            ConEmuTerminal terminal = new ConEmuTerminal(VSHelper.GetConEmuExePath(), arguments);
            terminal.title = $"SSH({launch.GetUser()}@{launch.GetHost()}:{launch.GetPort()})";
            return terminal;
        }

        public static ConEmuTerminal CreateSSHTerminalCommand(string remote_directory, string command)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            SSHLaunchOptions launch = Global.config.CreateSSHLaunchOptions();
            ///< arguments
            string arguments;
            if (command != null)
            {
                arguments = string.Format("-StartTSA -NoCloseConfirm -run \"{0}\" -o StrictHostKeyChecking=no -Z {1} -p {2} -t -t {3}@{4} \"cd \"{5}\" ; {6}\"",
                VSHelper.GetSshExePath(),
                launch.GetPassword(),
                launch.GetPort(),
                launch.GetUser(),
                launch.GetHost(),
                remote_directory,
                command);
            }
            else
            {
                arguments = string.Format("-StartTSA -NoCloseConfirm -run \"{0}\" -o StrictHostKeyChecking=no -Z {1} -p {2} -t -t {3}@{4} \"cd \"{5}\" ; bash\"",
                VSHelper.GetSshExePath(),
                launch.GetPassword(),
                launch.GetPort(),
                launch.GetUser(),
                launch.GetHost(),
                remote_directory);
            }

            ConEmuTerminal terminal = new ConEmuTerminal(VSHelper.GetConEmuExePath(), arguments);
            terminal.title = $"SSH({launch.GetUser()}@{launch.GetHost()}:{launch.GetPort()})";
            return terminal;
        }

        public static ConEmuTerminal CreateCMDTerminal(string path, string args = "", string envs = "")
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string working_directory = Config.GetDirectoryName(path);
            string file_name = Config.GetFileName(path);
            string arguments = $"-Dir \"{working_directory}\" -StartTSA -NoCloseConfirm -run cmd /K {envs}";
            if (!String.IsNullOrEmpty(file_name))
            {
                if (String.IsNullOrEmpty(args))
                {
                    arguments += $" & {file_name}";
                }
                else
                {
                    arguments += $" & {file_name} {args}";
                }
            }

            ConEmuTerminal terminal = new ConEmuTerminal(VSHelper.GetConEmuExePath(), arguments);
            terminal.title = "CMD";
            return terminal;
        }

        public ConEmuTerminal(string filename, string arguments)
        {
            this.process = new Process();
            this.process.EnableRaisingEvents = true;
            this.process.StartInfo.UseShellExecute = false;
            this.process.StartInfo.CreateNoWindow = true;
            this.process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            this.process.StartInfo.FileName = filename;
            this.process.StartInfo.Arguments = arguments;

            try
            {
                this.process.Start();
                this.process.WaitForInputIdle();
                this.main_hwnd = Win32.GetProcessTopWindow(this.process.Id, "VirtualConsoleClass", null);
                this.process.Exited += OnProcessExited;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public void Attach(Control parent_control)
        {
            if (this.parent_control != null)
            {
                this.parent_control.SizeChanged -= OnSizeChanged;
            }
            this.parent_control = parent_control;
            this.parent_control.SizeChanged += OnSizeChanged;
            if (this.process != null)
            {
                this.process.Exited -= OnProcessExited;
            }

            Win32.SetParent(this.main_hwnd, parent_control.Handle);

            Win32.RECT rect = Win32.AdjustWindowRectEx(parent_control.Handle, false);
            this.bound = Win32.AdjustWindowRectEx(this.main_hwnd, false);
            MoveWindow(this.bound.Left, this.bound.Top,
                Math.Abs(this.bound.Left) + Math.Abs(this.bound.Right) + parent_control.Width,
                Math.Abs(this.bound.Top) + Math.Abs(this.bound.Bottom) + parent_control.Height);
            ShowWindow(true);
            Win32.HideTrayIcon(this.process.Id);
        }

        public void Close()
        {
            if (this.process != null)
            {
                this.process.EnableRaisingEvents = false;
                int rv = Win32.PostMessage(this.main_hwnd, Win32.WM_CLOSE, 0, 0);
                if (!this.process.WaitForExit(1000))
                {
                    this.process.Kill();
                }
                this.process = null;
            }
        }

        public bool MoveWindow(int x, int y, int cx, int cy)
        {
            if (this.process != null && this.main_hwnd != IntPtr.Zero)
            {
                return Win32.SetWindowPos(this.main_hwnd, IntPtr.Zero, x, y, cx, cy, Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE | Win32.SWP_ASYNCWINDOWPOS);
            }
            return false;
        }

        public bool ShowWindow(bool show)
        {
            if (this.process != null && this.main_hwnd != IntPtr.Zero)
            {
                return Win32.SetWindowPos(this.main_hwnd, IntPtr.Zero, 0, 0, 0, 0,
                    Win32.SWP_NOSIZE | Win32.SWP_NOMOVE | Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE | Win32.SWP_ASYNCWINDOWPOS |
                    (show ? Win32.SWP_SHOWWINDOW : Win32.SWP_HIDEWINDOW));
            }
            return false;
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            if (this.parent_control != null)
            {
                MoveWindow(this.bound.Left, this.bound.Top,
                Math.Abs(this.bound.Left) + Math.Abs(this.bound.Right) + this.parent_control.Width,
                Math.Abs(this.bound.Top) + Math.Abs(this.bound.Bottom) + this.parent_control.Height);
            }
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                TerminalManager.Close(this);
            });
        }

        public string       title = "";
        public Process      process = null;
        private Control     parent_control = null;
        private IntPtr      main_hwnd = IntPtr.Zero;
        private Win32.RECT  bound;
    }
}
