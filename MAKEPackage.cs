using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using System.IO;

namespace MAKE
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(MAKEPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.EmptySolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class MAKEPackage : AsyncPackage, IVsShellPropertyEvents
    {
        /// <summary>
        /// MAKEPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "f74dd4b2-9825-4685-a9fc-782474c17500";

        public static MAKEPackage package_instance;

        private static Command command_instance;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            //await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            try
            {
                package_instance = this;
                await Global.InitializeAsync(this);
                command_instance = new Command();
                await command_instance.InitializeAsync(this);
            }
            catch (Exception ex)
            {   
                await VSHelper.MainThreadRunAsync(async delegate
                {
                    IVsCommandWindow commandWindow = Package.GetGlobalService(typeof(SVsCommandWindow)) as IVsCommandWindow;
                    commandWindow.PrintNoShow($"Error: {ex.Message}\r\n");
                });
            }
        }

        public int OnShellPropertyChange(int propid, object var)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
