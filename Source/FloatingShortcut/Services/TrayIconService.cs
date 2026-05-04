using System.Drawing;
using System.Windows.Forms;

namespace FloatingShortcut.Services;

/// <summary>
/// 系统托盘图标：显示运行状态，提供显示/隐藏窗口与退出。
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly ShortcutWindowManager _manager;
    private NotifyIcon? _notifyIcon;
    private bool _disposed;

    public TrayIconService(ShortcutWindowManager manager)
    {
        _manager = manager;
    }

    public void Initialize()
    {
        if (_notifyIcon is not null)
        {
            return;
        }

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "DeskPin Launcher",
            Visible = true
        };

        _notifyIcon.MouseDoubleClick += OnNotifyIconMouseDoubleClick;

        var menu = new ContextMenuStrip();
        menu.Items.Add("显示所有窗口", null, (_, _) => RunSafe(_manager.ShowAllWindows));
        menu.Items.Add("新建空白快捷窗口", null, (_, _) => RunSafe(_manager.NewBlankWindowFromTray));
        menu.Items.Add("隐藏所有窗口", null, (_, _) => RunSafe(_manager.HideAllWindows));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出程序", null, (_, _) => RunSafe(_manager.ExitApplication));

        _notifyIcon.ContextMenuStrip = menu;
    }

    private void OnNotifyIconMouseDoubleClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button != System.Windows.Forms.MouseButtons.Left)
        {
            return;
        }

        RunSafe(_manager.ShowAllWindows);
    }

    private static void RunSafe(Action action)
    {
        try
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher is not null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }
        catch
        {
            // 托盘操作不应导致进程崩溃
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_notifyIcon is not null)
        {
            try
            {
                _notifyIcon.MouseDoubleClick -= OnNotifyIconMouseDoubleClick;
                _notifyIcon.Visible = false;
                _notifyIcon.ContextMenuStrip?.Dispose();
                _notifyIcon.ContextMenuStrip = null;
                _notifyIcon.Dispose();
            }
            catch
            {
                // ignore
            }

            _notifyIcon = null;
        }
    }
}
