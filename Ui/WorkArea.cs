using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TrapShack.Ui;

/// <summary>
/// A borderless window maximises over the whole monitor and hides under the taskbar. This answers Windows'
/// WM_GETMINMAXINFO so a maximised window fills the monitor's work area (the screen minus the taskbar) instead.
/// </summary>
public static class WorkArea
{
    const int WM_GETMINMAXINFO = 0x0024;
    const uint MONITOR_DEFAULTTONEAREST = 2;

    public static void Hook(Window w)
    {
        var src = HwndSource.FromHwnd(new WindowInteropHelper(w).Handle);
        src?.AddHook(WndProc);
    }

    static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_GETMINMAXINFO) return IntPtr.Zero;
        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero) return IntPtr.Zero;
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info)) return IntPtr.Zero;

        var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
        var work = info.rcWork; var full = info.rcMonitor;
        mmi.ptMaxPosition.X = work.Left - full.Left;
        mmi.ptMaxPosition.Y = work.Top - full.Top;
        mmi.ptMaxSize.X = work.Right - work.Left;
        mmi.ptMaxSize.Y = work.Bottom - work.Top;
        Marshal.StructureToPtr(mmi, lParam, true);
        // leave handled = false so WPF still applies the window's MinWidth / MinHeight
        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    struct MINMAXINFO { public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize; }
    [StructLayout(LayoutKind.Sequential)]
    struct MONITORINFO { public int cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }

    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO info);
}
