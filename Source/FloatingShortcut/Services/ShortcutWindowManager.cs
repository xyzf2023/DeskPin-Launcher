using System.Windows;
using FloatingShortcut.Models;
using FloatingShortcut;

namespace FloatingShortcut.Services;

public sealed class ShortcutWindowManager
{
    private readonly ConfigService _configService = new();
    private readonly List<ShortcutWindow> _windows = new();
    private AppConfig _config = new();

    public ShellIconService ShellIcons { get; } = new();

    public LauncherService Launcher { get; } = new();

    public void Start()
    {
        _config = _configService.Load();
        _config.EnsureShortcuts();

        foreach (var sc in _config.Shortcuts)
        {
            OpenWindowFor(sc);
        }

        SaveAll();
    }

    public void SaveAll()
    {
        foreach (var w in _windows)
        {
            w.SyncToConfig();
        }

        _configService.Save(_config);
    }

    public void DuplicateWindow(ShortcutWindow source)
    {
        source.SyncToConfig();
        var size = source.Shortcut.WindowSize <= 0 ? 72 : source.Shortcut.WindowSize;
        var copy = new ShortcutConfig
        {
            Id = Guid.NewGuid().ToString("N"),
            TargetPath = source.Shortcut.TargetPath,
            WindowSize = size,
            WindowLeft = source.Left + 20,
            WindowTop = source.Top + 20
        };

        _config.Shortcuts.Add(copy);
        SaveAll();
        OpenWindowFor(copy);
    }

    public void NewBlankWindow(ShortcutWindow near)
    {
        near.SyncToConfig();
        var sc = new ShortcutConfig
        {
            Id = Guid.NewGuid().ToString("N"),
            TargetPath = null,
            WindowSize = 72,
            WindowLeft = near.Left + 20,
            WindowTop = near.Top + 20
        };

        _config.Shortcuts.Add(sc);
        SaveAll();
        OpenWindowFor(sc);
    }

    /// <summary>
    /// 二次启动等外部请求：在最后一个窗口右下偏移新建空白快捷，无窗口时用默认位置。
    /// </summary>
    public void NewBlankWindowFromExternalLaunch()
    {
        ShortcutWindow? reference = _windows.Count > 0 ? _windows[^1] : null;
        if (reference is not null)
        {
            reference.SyncToConfig();
        }

        var sc = new ShortcutConfig
        {
            Id = Guid.NewGuid().ToString("N"),
            TargetPath = null,
            WindowSize = 72,
            WindowLeft = reference is not null ? reference.Left + 20 : 100,
            WindowTop = reference is not null ? reference.Top + 20 : 100
        };

        _config.Shortcuts.Add(sc);
        SaveAll();
        OpenWindowFor(sc);
    }

    public void RemoveWindow(ShortcutWindow w)
    {
        if (_windows.Count == 1)
        {
            var result = MessageBox.Show(
                "这是最后一个快捷窗口，删除后将创建一个空白快捷窗口。是否继续？",
                "FloatingShortcut",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        w.SyncToConfig();
        _config.Shortcuts.RemoveAll(s => s.Id == w.Shortcut.Id);
        _windows.Remove(w);

        if (_config.Shortcuts.Count == 0)
        {
            _config.Shortcuts.Add(new ShortcutConfig { Id = Guid.NewGuid().ToString("N") });
        }

        SaveAll();
        w.InternalClose();

        if (_windows.Count == 0)
        {
            foreach (var sc in _config.Shortcuts)
            {
                OpenWindowFor(sc);
            }
        }
    }

    public void ExitApplication()
    {
        SaveAll();
        foreach (var w in _windows.ToList())
        {
            w.InternalClose();
        }

        _windows.Clear();
        Application.Current.Shutdown();
    }

    public void ShutdownOnExit()
    {
        SaveAll();
    }

    private void OpenWindowFor(ShortcutConfig sc)
    {
        var w = new ShortcutWindow(this, sc);
        _windows.Add(w);
        w.Show();
    }
}
