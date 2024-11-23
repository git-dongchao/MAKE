using System;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MAKE
{
    class TerminalManager
    {
        public static void CreateSSH(string path, string args, string envs)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Global.config.IsVSTerminal())
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
                {
                    ToolWindowPane terminal = await VSTerminal.CreateSSHTerminalAsync(path, args, envs);
                    if (terminal != null)
                    {
                        AddToolWindowPane(terminal);
                    }
                });
            }
            else if (Global.config.IsConEmuTerminal())
            {
                ConEmuTerminal terminal = ConEmuTerminal.CreateSSHTerminal(path, args, envs);
                if (terminal != null)
                {
                    terminal.ShowWindow(true);
                    conemu_terminals.Add(terminal);
                }
            }
            else
            {
                throw new InvalidOperationException("Error: terminal type error");
            }
        }

        public static void CreateSSHByCommand(string remote_directory, string command)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Global.config.IsVSTerminal())
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
                {
                    vs_debugger_terminal = await VSTerminal.CreateSSHTerminalCommandAsync(remote_directory, command);
                    if (vs_debugger_terminal != null)
                    {
                        AddToolWindowPane(vs_debugger_terminal);
                    }
                });
            }
            else if (Global.config.IsConEmuTerminal())
            {
                ConEmuTerminal terminal = ConEmuTerminal.CreateSSHTerminalCommand(remote_directory, command);
                if (terminal != null)
                {
                    terminal.ShowWindow(true);
                    conemu_terminals.Add(terminal);
                    conemu_debugger_terminal = terminal;
                }
            }
            else
            {
                throw new InvalidOperationException("Error: terminal type error");
            }
        }

        public static void CreateCMD(string path, string args = "", string envs = "")
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (Global.config.IsVSTerminal())
                {
                    ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
                    {
                        ToolWindowPane terminal = await VSTerminal.CreateCMDTerminalAsync(path, args, envs);
                        if (terminal != null)
                        {
                            AddToolWindowPane(terminal);
                        }
                    });
                }
                else if (Global.config.IsConEmuTerminal())
                {
                    ConEmuTerminal terminal = ConEmuTerminal.CreateCMDTerminal(path, args, envs);
                    if (terminal != null)
                    {
                        terminal.ShowWindow(true);
                        conemu_terminals.Add(terminal);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Error: terminal type error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void CloseAll()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            while (vs_terminals.Count > 0)
            {
                RemoveToolWindowPane(vs_terminals.Keys.First());
            }

            foreach (ConEmuTerminal terminal in conemu_terminals)
            {
                terminal.Close();
            }
            conemu_terminals.Clear();
        }

        public static bool Close(ConEmuTerminal terminal)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (conemu_debugger_terminal == terminal)
            {
                conemu_debugger_terminal = null;
            }
            if (conemu_terminals.Remove(terminal))
            {
                terminal.Close();
                return true;
            }
            return false;
        }

        public static void CloseDebug()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (vs_debugger_terminal != null)
            {
                RemoveToolWindowPane(vs_debugger_terminal);
            }

            if (conemu_debugger_terminal != null)
            {
                conemu_terminals.Remove(conemu_debugger_terminal);
                conemu_debugger_terminal.Close();
                conemu_debugger_terminal = null;
            }
        }

        public static void AddToolWindowPane(ToolWindowPane window_pane)
        {
            IVsWindowFrame2 frame = window_pane.Frame as IVsWindowFrame2;
            WindowFrameNotify notify = new WindowFrameNotify { window_pane = window_pane };
            frame.Advise(notify, out notify.cookie);
            vs_terminals.Add(window_pane, notify);
        }

        public static bool RemoveToolWindowPane(ToolWindowPane window_pane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (vs_debugger_terminal == window_pane)
            {
                vs_debugger_terminal = null;
            }

            IVsWindowFrame2 frame = window_pane.Frame as IVsWindowFrame2;
            if (vs_terminals.ContainsKey(window_pane))
            {
                WindowFrameNotify notify = vs_terminals[window_pane];
                frame.Unadvise(notify.cookie);
                vs_terminals.Remove(window_pane);
                VSTerminal.CloseTerminal(window_pane);
                ((IVsWindowFrame)(frame)).CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                return true;
            }
            return false;
        }

        class WindowFrameNotify : IVsWindowFrameNotify
        {
            public int OnShow(int fShow)
            {
                if (fShow == (int)__FRAMESHOW.FRAMESHOW_DestroyMultInst)
                {
                    RemoveToolWindowPane(this.window_pane);
                }
                return Microsoft.VisualStudio.VSConstants.S_OK;
            }
            public int OnMove()
            {
                return Microsoft.VisualStudio.VSConstants.S_OK;
            }
            public int OnSize()
            {
                return Microsoft.VisualStudio.VSConstants.S_OK;
            }
            public int OnDockableChange(int fDockable)
            {
                return Microsoft.VisualStudio.VSConstants.S_OK;
            }
            public int OnClose(ref uint pgrfSaveOptions)
            {
                RemoveToolWindowPane(this.window_pane);
                return Microsoft.VisualStudio.VSConstants.S_OK;
            }

            public ToolWindowPane window_pane = null;
            public uint cookie = 0;
        }

        private static ToolWindowPane vs_debugger_terminal = null;
        private static ConEmuTerminal conemu_debugger_terminal = null;
        private static Dictionary<ToolWindowPane, WindowFrameNotify> vs_terminals = new Dictionary<ToolWindowPane, WindowFrameNotify>();
        private static HashSet<ConEmuTerminal> conemu_terminals = new HashSet<ConEmuTerminal>();
    }
}
