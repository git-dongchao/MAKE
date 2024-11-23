using System;
using System.IO;
using System.Windows;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MAKE
{
    internal sealed class Command
    {
        public async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            OleMenuCommandService command_service = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (command_service == null)
            {
                throw new ArgumentNullException("IMenuCommandService");
            }

            // MAKE菜单/生成菜单
            {
                var command = new CommandID(CommandSet, CommandIds.ID_BUILD_START_SUBITEM);
                DynamicItemMenuCommand dynamicMenuCommand = new DynamicItemMenuCommand(command,
                    IsValidDynamicItemBuild, OnInvokedDynamicItemBuild, OnBeforeQueryStatusDynamicItemBuild);
                command_service.AddCommand(dynamicMenuCommand);
            }
            // MAKE菜单/调试菜单
            {
                var command = new CommandID(CommandSet, CommandIds.ID_DEBUG_START_SUBITEM);
                DynamicItemMenuCommand dynamicMenuCommand = new DynamicItemMenuCommand(command,
                    IsValidDynamicItemDebug, OnInvokedDynamicItemDebug, OnBeforeQueryStatusDynamicItemDebug);
                command_service.AddCommand(dynamicMenuCommand);
            }
            // MAKE菜单/运行菜单
            {
                var command = new CommandID(CommandSet, CommandIds.ID_RUN_START_SUBITEM);
                DynamicItemMenuCommand dynamicMenuCommand = new DynamicItemMenuCommand(command,
                    IsValidDynamicItemRun, OnInvokedDynamicItemRun, OnBeforeQueryStatusDynamicItemRun);
                command_service.AddCommand(dynamicMenuCommand);
            }
            // MAKE菜单/清除按钮
            {
                var command = new OleMenuCommand(OnInvokesClear, new CommandID(CommandSet, CommandIds.ID_CLEAR));
                command.BeforeQueryStatus += OnBeforeQueryStatusResetIntelliSense;
                command_service.AddCommand(command);
            }
            // MAKE菜单/IntelliSense环境子菜单
            {
                var command = new CommandID(CommandSet, CommandIds.ID_INTELLISENSE_ENV_START_SUBITEM);
                DynamicItemMenuCommand dynamicMenuCommand = new DynamicItemMenuCommand(command,
                    IsValidDynamicItemIntelliSenseEnv, OnInvokedDynamicItemIntelliSenseEnv, OnBeforeQueryStatusDynamicItemIntelliSenseEnv);
                command_service.AddCommand(dynamicMenuCommand);
            }
            // MAKE菜单/重置IntelliSense按钮
            {
                var command = new OleMenuCommand(OnInvokesResetIntelliSense, new CommandID(CommandSet, CommandIds.ID_RESET_INTELLISENSE));
                command.BeforeQueryStatus += OnBeforeQueryStatusResetIntelliSense;
                command_service.AddCommand(command);
            }
            // MAKE菜单/设置按钮
            {
                var command = new OleMenuCommand(OnInvokeOption, new CommandID(CommandSet, CommandIds.ID_SETTINGS_BUTTON));
                command.BeforeQueryStatus += OnBeforeQueryStatusOption;
                command_service.AddCommand(command);
            }
            // 右键菜单/生成按钮
            {
                var command = new OleMenuCommand(OnInvokeBuild, new CommandID(CommandSet, CommandIds.ID_BUILD_CONTEXT_MENU));
                command.BeforeQueryStatus += OnBeforeQueryStatusBuild;
                command_service.AddCommand(command);
            }
            // 右键菜单/调试按钮
            {
                var command = new OleMenuCommand(OnInvokeDebug, new CommandID(CommandSet, CommandIds.ID_DEBUG_CONTEXT_MENU));
                command.BeforeQueryStatus += OnBeforeQueryStatusDebug;
                command_service.AddCommand(command);
            }
            // 右键菜单/运行按钮
            {
                var command = new OleMenuCommand(OnInvokeRun, new CommandID(CommandSet, CommandIds.ID_RUN_CONTEXT_MENU));
                command.BeforeQueryStatus += OnBeforeQueryStatusRun;
                command_service.AddCommand(command);
            }
            // 右键菜单/WindowsTerminal按钮
            {
                var command = new OleMenuCommand(OnInvokeWindowsTerminal, new CommandID(CommandSet, CommandIds.ID_WINDOWS_TERMINAL_CONTEXT_MENU));
                command.BeforeQueryStatus += OnBeforeQueryStatusWindowsTerminal;
                command_service.AddCommand(command);
            }
            // 右键菜单/LinuxTerminal按钮
            {
                var command = new OleMenuCommand(OnInvokeLinuxTerminal, new CommandID(CommandSet, CommandIds.ID_LINUX_TERMINAL_CONTEXT_MENU));
                command.BeforeQueryStatus += OnBeforeQueryStatusLinuxTerminal;
                command_service.AddCommand(command);
            }
        }

        private bool IsValidDynamicItemBuild(int commandId)
        {
            return (commandId >= (int)CommandIds.ID_BUILD_START_SUBITEM) &&
                (commandId - (int)CommandIds.ID_BUILD_START_SUBITEM < Global.config.build_projects.Count);
        }
        private void OnInvokedDynamicItemBuild(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is DynamicItemMenuCommand cmd)
            {
                BuildProject.Start(Global.config.GetBuildProject(cmd.Data));
            }
        }
        private void OnBeforeQueryStatusDynamicItemBuild(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DynamicItemMenuCommand matchedCommand = (DynamicItemMenuCommand)sender;
            matchedCommand.Enabled = (VSHelper.IsPackageEnable(Global.dte2) && !DebugProject.IsDebugger());
            matchedCommand.Visible = false;

            int CommandId = (int)CommandIds.ID_BUILD_START_SUBITEM;
            foreach (var key_val in Global.config.build_projects)
            {
                if (matchedCommand.MatchedCommandId != 0)
                {
                    if (CommandId == matchedCommand.MatchedCommandId)
                    {
                        matchedCommand.Visible = true;
                        matchedCommand.Data = key_val.Key;
                        matchedCommand.Text = $"{CommandId - CommandIds.ID_BUILD_START_SUBITEM + 1} {key_val.Key}";
                        break;
                    }
                }
                else if (CommandId == matchedCommand.CommandID.ID)
                {
                    matchedCommand.Visible = true;
                    matchedCommand.Data = key_val.Key;
                    matchedCommand.Text = $"{CommandId - CommandIds.ID_BUILD_START_SUBITEM + 1} {key_val.Key}";
                    break;
                }
                ++CommandId;
            }
            matchedCommand.MatchedCommandId = 0;
        }

        private bool IsValidDynamicItemDebug(int commandId)
        {
            return (commandId >= (int)CommandIds.ID_DEBUG_START_SUBITEM) &&
                (commandId - (int)CommandIds.ID_DEBUG_START_SUBITEM < Global.config.targets.Count);
        }
        private void OnInvokedDynamicItemDebug(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is DynamicItemMenuCommand cmd)
            {
                var project = Global.config.GetDebugProject(cmd.Data);
                if (project != null)
                {
                    DebugProject.Start(project.target, project.args, project.envs);
                }
            }

        }
        private void OnBeforeQueryStatusDynamicItemDebug(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DynamicItemMenuCommand matchedCommand = (DynamicItemMenuCommand)sender;
            matchedCommand.Enabled = (VSHelper.IsPackageEnable(Global.dte2) && !DebugProject.IsDebugger());
            matchedCommand.Visible = false;

            int CommandId = (int)CommandIds.ID_DEBUG_START_SUBITEM;
            foreach (var key_val in Global.config.targets)
            {
                if (matchedCommand.MatchedCommandId != 0)
                {
                    if (CommandId == matchedCommand.MatchedCommandId)
                    {
                        matchedCommand.Visible = true;
                        matchedCommand.Data = key_val.Key;
                        matchedCommand.Text = $"{CommandId - CommandIds.ID_DEBUG_START_SUBITEM + 1} {key_val.Key}";
                        break;
                    }
                }
                else if (CommandId == matchedCommand.CommandID.ID)
                {
                    matchedCommand.Visible = true;
                    matchedCommand.Data = key_val.Key;
                    matchedCommand.Text = $"{CommandId - CommandIds.ID_DEBUG_START_SUBITEM + 1} {key_val.Key}";
                    break;
                }
                ++CommandId;
            }
            matchedCommand.MatchedCommandId = 0;
        }

        private bool IsValidDynamicItemRun(int commandId)
        {
            return (commandId >= (int)CommandIds.ID_RUN_START_SUBITEM) &&
                (commandId - (int)CommandIds.ID_RUN_START_SUBITEM < Global.config.targets.Count);
        }
        private void OnInvokedDynamicItemRun(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is DynamicItemMenuCommand cmd)
            {
                var project = Global.config.GetRunProject(cmd.Data);
                if (project != null)
                {
                    RunProject.Start(project.target, project.args, project.envs);
                }
            }

        }
        private void OnBeforeQueryStatusDynamicItemRun(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DynamicItemMenuCommand matchedCommand = (DynamicItemMenuCommand)sender;
            matchedCommand.Enabled = (VSHelper.IsPackageEnable(Global.dte2) && !DebugProject.IsDebugger());
            matchedCommand.Visible = false;

            int CommandId = (int)CommandIds.ID_RUN_START_SUBITEM;
            foreach (var key_val in Global.config.targets)
            {
                if (matchedCommand.MatchedCommandId != 0)
                {
                    if (CommandId == matchedCommand.MatchedCommandId)
                    {
                        matchedCommand.Visible = true;
                        matchedCommand.Data = key_val.Key;
                        matchedCommand.Text = $"{CommandId - CommandIds.ID_RUN_START_SUBITEM + 1} {key_val.Key}";
                        break;
                    }
                }
                else if (CommandId == matchedCommand.CommandID.ID)
                {
                    matchedCommand.Visible = true;
                    matchedCommand.Data = key_val.Key;
                    matchedCommand.Text = $"{CommandId - CommandIds.ID_RUN_START_SUBITEM + 1} {key_val.Key}";
                    break;
                }
                ++CommandId;
            }
            matchedCommand.MatchedCommandId = 0;
        }

        private void OnBeforeQueryStatusClear(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand cmd)
            {
                cmd.Enabled = (VSHelper.IsPackageEnable(Global.dte2) && !DebugProject.IsDebugger());
                cmd.Visible = true;
            }
        }
        private void OnInvokesClear(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (VSHelper.ShowMessageBox("", "是否清除", OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGICON.OLEMSGICON_WARNING) == (int)MessageBoxResult.OK)
            {
                Global.config.build_projects.Clear();
                Global.config.targets.Clear();
                Global.config.Save();
            }
        }

        private bool IsValidDynamicItemIntelliSenseEnv(int commandId)
        {
            return (commandId >= (int)CommandIds.ID_INTELLISENSE_ENV_START_SUBITEM) &&
                (commandId - (int)CommandIds.ID_INTELLISENSE_ENV_START_SUBITEM < Global.config.intellisense_directory.Count);
        }
        private void OnInvokedDynamicItemIntelliSenseEnv(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is DynamicItemMenuCommand cmd)
            {
                Global.config.intellisense_environment = cmd.Data;
                Global.config.CreateCppProperties().Save();

                Global.dte2.get_Properties("TextEditor", "C/C++ Specific").Item("RecreateDatabase").Value = true;

                if (VSHelper.ShowMessageBox("", "保存所有文件，重新加载", OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGICON.OLEMSGICON_WARNING) == (int)MessageBoxResult.OK)
                {
                    Global.dte2.ExecuteCommand("File.SaveAll");
                    VSHelper.ReloadSolution(Global.dte2);
                }
            }
        }
        private void OnBeforeQueryStatusDynamicItemIntelliSenseEnv(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DynamicItemMenuCommand matchedCommand = (DynamicItemMenuCommand)sender;
            matchedCommand.Enabled = (VSHelper.IsPackageEnable(Global.dte2) && !DebugProject.IsDebugger());
            matchedCommand.Visible = false;

            int CommandId = (int)CommandIds.ID_INTELLISENSE_ENV_START_SUBITEM;
            foreach (var key_val in Global.config.intellisense_directory)
            {
                if (matchedCommand.MatchedCommandId != 0)
                {
                    if (CommandId == matchedCommand.MatchedCommandId)
                    {
                        matchedCommand.Visible = true;
                        matchedCommand.Checked = (key_val.Key == Global.config.intellisense_environment);
                        matchedCommand.Data = key_val.Key;
                        matchedCommand.Text = $"{CommandId - CommandIds.ID_INTELLISENSE_ENV_START_SUBITEM + 1} {key_val.Key}";
                        break;
                    }
                }
                else if (CommandId == matchedCommand.CommandID.ID)
                {
                    matchedCommand.Visible = true;
                    matchedCommand.Checked = (key_val.Key == Global.config.intellisense_environment);
                    matchedCommand.Data = key_val.Key;
                    matchedCommand.Text = $"{CommandId - CommandIds.ID_INTELLISENSE_ENV_START_SUBITEM + 1} {key_val.Key}";
                    break;
                }
                ++CommandId;
            }
            matchedCommand.MatchedCommandId = 0;
        }


        private void OnBeforeQueryStatusResetIntelliSense(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand cmd)
            {
                cmd.Enabled = (VSHelper.IsPackageEnable(Global.dte2) && !DebugProject.IsDebugger());
                cmd.Visible = true;
            }
        }
        private void OnInvokesResetIntelliSense(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Global.config.CreateCppProperties().Save();

            //Environment
            //Environment - Documents
            //Projects - VBDefaults
            //Projects - VCGeneral
            //TextEditor - AllLanguages
            //TextEditor - Basic
            //TextEditor - Basic-Specific
            //TextEditor - C/C++
            //TextEditor - C/C++ Specific
            Global.dte2.get_Properties("TextEditor", "C/C++ Specific").Item("RecreateDatabase").Value = true;
            if (VSHelper.ShowMessageBox("", "保存所有文件，重新启动", OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGICON.OLEMSGICON_WARNING) == (int)MessageBoxResult.OK)
            {
                Global.dte2.ExecuteCommand("File.SaveAll");
                VSHelper.Restart();
            }
        }


        private void OnBeforeQueryStatusOption(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand cmd)
            {
                cmd.Enabled = (VSHelper.IsPackageEnable(Global.dte2) && !DebugProject.IsDebugger());
                cmd.Visible = true;
            }
        }
        private void OnInvokeOption(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Global.config.Load();
            string target_machine = Global.config.target_machine;
            OptionsSettingWindow window = new OptionsSettingWindow(Global.config);
            if (window.ShowDialog() == true)
            {
                Global.config.Save();
                if (target_machine != Global.config.target_machine)
                {
                    Global.dte2.get_Properties("TextEditor", "C/C++ Specific").Item("RecreateDatabase").Value = true;
                    if (VSHelper.ShowMessageBox("", "保存所有文件，重新加载", OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGICON.OLEMSGICON_WARNING) == (int)MessageBoxResult.OK)
                    {
                        Global.dte2.ExecuteCommand("File.SaveAll");
                        VSHelper.ReloadSolution(Global.dte2);
                    }
                }
            }
        }

        private void OnBeforeQueryStatusBuild(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand cmd)
            {
                var selected_target = VSHelper.GetCurrentSelection();
                cmd.Enabled = BuildProject.IsEnable(selected_target) && !DebugProject.IsDebugger();
            }
        }
        private void OnInvokeBuild(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var selected_target = VSHelper.GetCurrentSelection();
            Global.config.AddBuildProject(Path.GetFileName(selected_target), Config.GetRelativePath(selected_target));
            Global.config.Save();
            BuildProject.Start(selected_target);
        }

        private void OnBeforeQueryStatusDebug(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand cmd)
            {
                var selected_target = VSHelper.GetCurrentSelection();
                cmd.Enabled = DebugProject.IsEnable(selected_target) && !DebugProject.IsDebugger();
            }
        }
        private void OnInvokeDebug(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var selected_target = VSHelper.GetCurrentSelection();
            var relative_target = Config.GetRelativePath(selected_target);
            var project = Global.config.GetDebugProject(relative_target);
            if (project == null)
            {
                if (Global.config.AddDebugProject(relative_target) != null)
                {
                    string filename = Global.config.Save();
                    Global.dte2.ItemOperations.OpenFile(filename, EnvDTE.Constants.vsViewKindTextView);
                    Global.dte2.ActiveDocument.Activate();
                }
                return;
            }
            DebugProject.Start(project.target, project.args, project.envs);
        }

        private void OnBeforeQueryStatusRun(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand cmd)
            {
                var selected_target = VSHelper.GetCurrentSelection();
                cmd.Enabled = RunProject.IsEnable(selected_target);
            }
        }
        private void OnInvokeRun(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var selected_target = VSHelper.GetCurrentSelection();
            var relative_target = Config.GetRelativePath(selected_target);
            var project = Global.config.GetRunProject(relative_target);
            if (project == null)
            {
                if (Global.config.AddRunProject(relative_target) != null)
                {
                    string filename = Global.config.Save();
                    Global.dte2.ItemOperations.OpenFile(filename, EnvDTE.Constants.vsViewKindTextView);
                    Global.dte2.ActiveDocument.Activate();
                }
                return;
            }
            RunProject.Start(project.target, project.args, project.envs);
        }

        private void OnBeforeQueryStatusWindowsTerminal(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand cmd)
            {
                var selected_target = VSHelper.GetCurrentSelection();
                cmd.Enabled = VSTerminal.IsCMDEnable(selected_target);
            }
        }
        private void OnInvokeWindowsTerminal(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var selected_target = VSHelper.GetCurrentSelection();
            TerminalManager.CreateCMD(selected_target, "", Global.WindowsEnvironment());
        }

        private void OnBeforeQueryStatusLinuxTerminal(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand cmd)
            {
                var selected_target = VSHelper.GetCurrentSelection();
                cmd.Enabled = VSTerminal.IsSSHEnable(selected_target);
            }
        }
        private void OnInvokeLinuxTerminal(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var selected_target = VSHelper.GetCurrentSelection();
            TerminalManager.CreateSSH(selected_target, "", Global.LinuxEnvironment());
        }

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d04c3aba-8f9f-469a-86f8-4f071d420f9b");

        private sealed class CommandIds
        {
            public const int ID_BUILD_START_SUBITEM = 0x1103;            // MAKE菜单/生成菜单/子按钮
            public const int ID_BUILD_END_SUBITEM = 0x1143;

            public const int ID_DEBUG_START_SUBITEM = 0x1203;            // MAKE菜单/调试菜单/子按钮
            public const int ID_DEBUG_END_SUBITEM = 0x1243;

            public const int ID_RUN_START_SUBITEM = 0x1303;             // MAKE菜单/运行菜单/子按钮
            public const int ID_RUN_END_SUBITEM = 0x1343;

            public const int ID_CLEAR = 0x1390;

            public const int ID_INTELLISENSE_ENV_START_SUBITEM = 0x1403;// MAKE菜单/IntelliSense环境菜单/子按钮
            public const int ID_INTELLISENSE_ENV_END_SUBITEM = 0x1443;
            public const int ID_RESET_INTELLISENSE = 0x1451;            // MAKE菜单/重置IntelliSense按钮
            public const int ID_SETTINGS_BUTTON = 0x1501;               // MAKE菜单/设置按钮

            public const int ID_BUILD_CONTEXT_MENU = 0x2002;             // 右键菜单/生成按钮 
            public const int ID_DEBUG_CONTEXT_MENU = 0x2003;             // 右键菜单/调试按钮
            public const int ID_RUN_CONTEXT_MENU = 0x2004;               // 右键菜单/运行按钮
            public const int ID_WINDOWS_TERMINAL_CONTEXT_MENU = 0x2005;  // 右键菜单/Window按钮
            public const int ID_LINUX_TERMINAL_CONTEXT_MENU = 0x2006;    // 右键菜单/Linux按钮
        }
    }
}
