using System;
using System.Runtime.InteropServices;

partial class Win32
{
    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;
    public const int WM_CLOSE = 0x10;
    public const int WS_CHILD = 0x40000000;

    public struct RECT 
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom; 
    }

    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetParent(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("User32.dll", SetLastError = true)]
    public static extern int PostMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetFocus(IntPtr hWnd);

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const uint SWP_HIDEWINDOW = 0x0080;
    public const uint SWP_ASYNCWINDOWPOS = 0x4000;
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int EnumChildWindows(IntPtr hWndParent, EnumWindowsCallBack lpfn, int lParam);
    public delegate bool EnumWindowsCallBack(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int ID);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetForegroundWindow();

    public static IntPtr GetProcessTopWindow(int process, string windowClass, string windowTitle)
    {
        IntPtr hwnd = IntPtr.Zero;
        do
        {
            hwnd = FindWindowEx(IntPtr.Zero, hwnd, windowClass, windowTitle);
            if (hwnd != IntPtr.Zero)
            {
                int id = 0;
                GetWindowThreadProcessId(hwnd, out id);
                if (id == process)
                {
                    return hwnd;
                }
            }
        } while (hwnd != IntPtr.Zero);
        return hwnd;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AdjustWindowRectEx(out RECT lpRect, int dwStyle, bool bMenu, int dwExStyle);

    public static Win32.RECT AdjustWindowRectEx(IntPtr hwnd, bool bMenu)
    {
        Win32.RECT rect = new Win32.RECT();
        bool rv = AdjustWindowRectEx(out rect, Win32.GetWindowLong(hwnd, GWL_STYLE), bMenu, GetWindowLong(hwnd, GWL_EXSTYLE));
        if (!rv) {
            throw new Exception("call AdjustWindowRectEx fail");
        }
        return rect;
    }
};