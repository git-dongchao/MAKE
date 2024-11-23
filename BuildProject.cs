using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MAKE
{
    class BuildProject
    {
        public static void Start(string file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var extension = Path.GetExtension(file);
            if (extension == ".bat")
            {
                TerminalManager.CreateCMD(file, "", Global.WindowsEnvironment());
            }
            else if (extension == ".sh")
            {
                TerminalManager.CreateSSH(file, "", Global.LinuxEnvironment());
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
                if (extension == ".bat" || extension == ".sh")
                {
                    return true;
                }
            }
            return false;
        }
    }
}
