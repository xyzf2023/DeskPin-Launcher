using System.Windows;
using FloatingShortcut.Services;

namespace FloatingShortcut;

public partial class App : System.Windows.Application
{
    private ShortcutWindowManager? _manager;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        _manager = new ShortcutWindowManager();
        _manager.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _manager?.ShutdownOnExit();
        base.OnExit(e);
    }
}

