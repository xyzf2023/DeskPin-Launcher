using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FloatingShortcut.Services;

public sealed class ShellIconService
{
    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiLargeIcon = 0x000000000;
    private const uint ShgfiUseFileAttributes = 0x000000010;
    private const uint FileAttributeNormal = 0x00000080;
    private const uint FileAttributeDirectory = 0x00000010;

    public ImageSource GetIconForPath(string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return GetDefaultIcon();
        }

        try
        {
            if (Directory.Exists(targetPath))
            {
                return GetShellIcon(targetPath, FileAttributeDirectory, false) ?? GetDefaultIcon();
            }

            if (File.Exists(targetPath))
            {
                return GetShellIcon(targetPath, FileAttributeNormal, false) ?? GetDefaultIcon();
            }
        }
        catch
        {
            // 忽略并回退默认图标
        }

        return GetDefaultIcon();
    }

    public ImageSource GetDefaultIcon()
    {
        using var icon = SystemIcons.Application;
        return CreateImageSource(icon);
    }

    private static ImageSource? GetShellIcon(string path, uint attribute, bool useAttributeOnly)
    {
        var flags = ShgfiIcon | ShgfiLargeIcon;
        if (useAttributeOnly)
        {
            flags |= ShgfiUseFileAttributes;
        }

        var fileInfo = new SHFILEINFO();
        var result = SHGetFileInfo(path, attribute, ref fileInfo, (uint)Marshal.SizeOf(fileInfo), flags);
        if (result == IntPtr.Zero || fileInfo.hIcon == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            using var icon = Icon.FromHandle(fileInfo.hIcon);
            return CreateImageSource(icon);
        }
        finally
        {
            DestroyIcon(fileInfo.hIcon);
        }
    }

    private static ImageSource CreateImageSource(Icon icon)
    {
        var image = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(48, 48));
        image.Freeze();
        return image;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }
}
