using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MAKE
{
    class RunProject
    {
        public static void Start(string file, string args, string envs)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var extension = Path.GetExtension(file);
            if (extension == ".exe")
            {
                TerminalManager.CreateCMD(file, args, Global.WindowsEnvironment(envs));
            }
            else if (extension == "")
            {
                TerminalManager.CreateSSH(file, args, Global.LinuxEnvironment(envs));
            }
            else
            {
                VSHelper.ShowMessageBox("", "不支持的文件类型", OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGICON.OLEMSGICON_WARNING);
            }
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
    }
}
