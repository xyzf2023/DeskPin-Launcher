using System.Diagnostics;
using System.IO;

namespace FloatingShortcut.Services;

public sealed class LauncherService
{
    public void Launch(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new InvalidOperationException("未绑定目标路径。");
        }

        if (!File.Exists(targetPath) && !Directory.Exists(targetPath))
        {
            throw new FileNotFoundException("目标不存在。", targetPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = targetPath,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    public void OpenContainingLocation(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new InvalidOperationException("未绑定目标路径。");
        }

        if (Directory.Exists(targetPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = targetPath,
                UseShellExecute = true
            });
            return;
        }

        if (File.Exists(targetPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{targetPath}\"",
                UseShellExecute = true
            });
            return;
        }

        throw new FileNotFoundException("目标不存在。", targetPath);
    }
}
