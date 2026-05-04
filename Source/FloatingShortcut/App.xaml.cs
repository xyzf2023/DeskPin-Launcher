using System.Windows;
using FloatingShortcut.Services;

namespace FloatingShortcut;

public partial class App : System.Windows.Application
{
    private SingleInstanceService? _singleInstance;
    private ShortcutWindowManager? _manager;
    private TrayIconService? _trayIconService;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        _singleInstance = SingleInstanceService.TryAcquirePrimary();
        if (_singleInstance is null)
        {
            if (!SingleInstanceService.TryNotifyNewBlankWindow())
            {
                System.Windows.MessageBox.Show(
                    "无法通知已运行的 FloatingShortcut，请手动右键新建窗口。",
                    "FloatingShortcut",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            Shutdown();
            return;
        }

        _manager = new ShortcutWindowManager();
        _trayIconService = new TrayIconService(_manager);
        _manager.SetTrayIconService(_trayIconService);
        _manager.Start();
        _singleInstance.StartPipeServer(_manager);
        _trayIconService.Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _manager?.ShutdownOnExit();
        _trayIconService?.Dispose();
        _singleInstance?.Stop();
        base.OnExit(e);
    }
}

