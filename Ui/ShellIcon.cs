using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TrapShack.Ui;

/// <summary>The icon Windows shows for a file (works for programs and shortcuts), as a WPF image.</summary>
public static class ShellIcon
{
    public static ImageSource? For(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var info = new SHFILEINFO();
        try
        {
            var r = SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_LARGEICON);
            if (r == IntPtr.Zero || info.hIcon == IntPtr.Zero) return null;
            var img = Imaging.CreateBitmapSourceFromHIcon(info.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            img.Freeze();
            return img;
        }
        catch { return null; }
        finally { if (info.hIcon != IntPtr.Zero) DestroyIcon(info.hIcon); }
    }

    const uint SHGFI_ICON = 0x100, SHGFI_LARGEICON = 0x0;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SHGetFileInfo(string path, uint attrs, ref SHFILEINFO info, uint size, uint flags);

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr hIcon);
}
