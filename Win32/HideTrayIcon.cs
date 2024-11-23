using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

partial class Win32
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MY_TBBUTTON
    {
        public int iBitmap;
        public int idCommand;
        public byte fsState;
        public byte fsStyle;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] bReserved;
        public UInt64 dwData;
        public UInt64 iString;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MY_TRAYDATA
    {
        public UInt64 hwnd;
        public uint uID;
        public uint uCallbackMessage;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] Reserved;
        public UInt64 hIcon;
    }

    
    [StructLayout(LayoutKind.Sequential)]
    public struct MY_NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [Flags]
    public enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VirtualMemoryOperation = 0x00000008,
        VirtualMemoryRead = 0x00000010,
        VirtualMemoryWrite = 0x00000020,
        DuplicateHandle = 0x00000040,
        CreateProcess = 0x000000080,
        SetQuota = 0x00000100,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        QueryLimitedInformation = 0x00001000,
        Synchronize = 0x00100000
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint nSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [Flags]
    public enum AllocationType : uint
    {
        Commit = 0x00001000,
        Reserve = 0x00002000,
        Reset = 0x00080000,
        TopDown = 0x00100000,
        WriteWatch = 0x00200000,
        Physical = 0x00400000,
        LargePages = 0x20000000,
        FourMbPages = 0x80000000
    }

    [Flags]
    public enum MemoryProtection : uint
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierflag = 0x100,
        NoCacheModifierflag = 0x200,
        WriteCombineModifierflag = 0x400
    }

    [Flags]
    public enum MemoryFreeType : uint
    {
        MEM_DECOMMIT = 0x4000,
        MEM_RELEASE = 0x8000
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, MemoryFreeType dwFreeType);

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref MY_NOTIFYICONDATA pnid);

    public static void HideTrayIcon(int process_id)
    {
        IntPtr hWnd = IntPtr.Zero;
        IntPtr Shell_TrayWnd = Win32.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
        IntPtr TrayNotifyWnd = Win32.FindWindowEx(Shell_TrayWnd, IntPtr.Zero, "TrayNotifyWnd", null);
        IntPtr SysPager = Win32.FindWindowEx(TrayNotifyWnd, IntPtr.Zero, "SysPager", null);
        List<IntPtr> ToolbarWindow32 = new List<IntPtr>();
        if (SysPager != IntPtr.Zero)
        {
            hWnd = Win32.FindWindowEx(SysPager, IntPtr.Zero, "ToolbarWindow32", null);
            if (hWnd != IntPtr.Zero)
            {
                ToolbarWindow32.Add(hWnd);
            }
        }
        else
        {
            hWnd = Win32.FindWindowEx(TrayNotifyWnd, IntPtr.Zero, "ToolbarWindow32", null);
            if (hWnd != IntPtr.Zero)
            {
                ToolbarWindow32.Add(hWnd);
            }
        }

        IntPtr hNotifyIconOverflowWindow = Win32.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "NotifyIconOverflowWindow", null);
        if (hNotifyIconOverflowWindow != IntPtr.Zero)
        {
            hWnd = Win32.FindWindowEx(hNotifyIconOverflowWindow, IntPtr.Zero, "ToolbarWindow32", null);
            if (hWnd != IntPtr.Zero)
            {
                ToolbarWindow32.Add(hWnd);
            }
        }

        List<MY_TRAYDATA> tTrayDatas = new List<MY_TRAYDATA>();
        foreach (var Toolbar in ToolbarWindow32)
        {
            tTrayDatas.AddRange(GetTrayDataList(Toolbar));
        }
        foreach (MY_TRAYDATA TrayData in tTrayDatas)
        {
            int dwProcessId = 0;
            Win32.GetWindowThreadProcessId(new IntPtr((long)TrayData.hwnd), out dwProcessId);
            if (dwProcessId == process_id)
            {
                const uint NIF_STATE = 0x00000008;
                const uint NIS_HIDDEN = 0x00000001;
                const int NIM_MODIFY = 0x00000001;
                MY_NOTIFYICONDATA NotifyIconData = new MY_NOTIFYICONDATA();
                NotifyIconData.cbSize = Marshal.SizeOf<MY_NOTIFYICONDATA>();
                NotifyIconData.hWnd = new IntPtr((long)TrayData.hwnd);
                NotifyIconData.uID = TrayData.uID;
                NotifyIconData.uFlags = NIF_STATE;
                NotifyIconData.dwState = (uint)NIS_HIDDEN;
                NotifyIconData.dwStateMask = NIS_HIDDEN;
                bool success = Shell_NotifyIcon(NIM_MODIFY, ref NotifyIconData);
            }
        }
    }

    private static List<MY_TRAYDATA> GetTrayDataList(IntPtr toolbar_window32)
    {
        const int TB_GETBUTTON = 0x0417;
        const int TB_BUTTONCOUNT = 0x0418;

        int nButtonCount = Win32.SendMessage(toolbar_window32, TB_BUTTONCOUNT, IntPtr.Zero, IntPtr.Zero);
        
        int dwProcessId = 0;
        int nSize = Math.Max(Marshal.SizeOf<MY_TBBUTTON>(), Marshal.SizeOf<MY_TBBUTTON>() * nButtonCount);
        nSize = ((nSize + 1023) / 1024) * 1024;
        Win32.GetWindowThreadProcessId(toolbar_window32, out dwProcessId);
        IntPtr hProcess = Win32.OpenProcess(ProcessAccessFlags.VirtualMemoryOperation | ProcessAccessFlags.VirtualMemoryRead | ProcessAccessFlags.VirtualMemoryWrite, false, dwProcessId);
        IntPtr lpAddress = Win32.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)nSize, AllocationType.Commit, MemoryProtection.ReadWrite);

        List<MY_TRAYDATA> tTrayDatas = new List<MY_TRAYDATA>();
        for (int i = 0; i < nButtonCount; ++i)
        {
            int nResult = Win32.SendMessage(toolbar_window32, TB_GETBUTTON, new IntPtr(i), lpAddress);
            if (nResult != 0)
            {
                MY_TBBUTTON? tButton = Win32.ReadProcessMemoryToStruct<MY_TBBUTTON>(hProcess, lpAddress);
                if (tButton != null)
                {
                    MY_TRAYDATA? tTrayData = Win32.ReadProcessMemoryToStruct<MY_TRAYDATA>(hProcess, new IntPtr((long)tButton.Value.dwData));
                    if (tTrayData != null)
                    {
                        tTrayDatas.Add(tTrayData.Value);
                    }
                }
            }
        }

        Win32.VirtualFreeEx(hProcess, lpAddress, (uint)nSize, MemoryFreeType.MEM_RELEASE);
        Win32.CloseHandle(hProcess);

        return tTrayDatas;
    }

    private static T? ReadProcessMemoryToStruct<T>(IntPtr hProcess, IntPtr lpBaseAddress) where T : struct
    {
        int lpNumberOfBytesRead = 0;
        byte[] buffer = new byte[Marshal.SizeOf<T>()];
        bool success = Win32.ReadProcessMemory(hProcess, lpBaseAddress, buffer, (uint)buffer.Length, out lpNumberOfBytesRead);
        if (success && buffer.Length == lpNumberOfBytesRead)
        {
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                return Marshal.PtrToStructure<T>(ptr);
            }
            finally
            {
                handle.Free();
            }
        }
        return null;
    }
}
