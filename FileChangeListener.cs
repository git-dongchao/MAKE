using System;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MAKE
{
    public class FileEventArgs : EventArgs
    {
        public FileEventArgs(string file)
        {
            this.file = file;
        }

        public string file { get; }
    }

    class FileChangeListener : IVsFileChangeEvents
    {
        public FileChangeListener()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.file_change = VSHelper.GetFileChangeEx();
        }

        public void Subscribe(string file)
        {
            if (!String.IsNullOrEmpty(file) && !this.event_cookies.ContainsKey(file))
            {
                ErrorHandler.ThrowOnFailure(
                    this.file_change.AdviseFileChange(file, (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Time), this, out var cookie));
                this.event_cookies.Add(file, cookie);
            }
        }

        public void Unsubscribe(string file)
        {
            if (!String.IsNullOrEmpty(file) && this.event_cookies.TryGetValue(file, out var cookie))
            {
                ErrorHandler.ThrowOnFailure(this.file_change.UnadviseFileChange(cookie));
                this.event_cookies.Remove(file);
            }
        }

        public void UnsubscribeAll()
        {
            foreach(var key_value in this.event_cookies)
            {
                ErrorHandler.ThrowOnFailure(this.file_change.UnadviseFileChange(key_value.Value));
            }
            this.event_cookies.Clear();
        }

        public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            foreach(var file in rgpszFile)
            {
                this.OnFileChange(this, new FileEventArgs(file));
            }
            return VSConstants.S_OK;
        }

        public int DirectoryChanged(string pszDirectory)
        {
            return VSConstants.S_OK;
        }

        private IVsFileChangeEx file_change = null;
        private Dictionary<string, uint> event_cookies = new Dictionary<string, uint>();
        public event EventHandler<FileEventArgs> OnFileChange;
    }
}
