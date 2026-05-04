using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FloatingShortcut.Models;
using FloatingShortcut.Services;
using Microsoft.Win32;

namespace FloatingShortcut;

public partial class ShortcutWindow : Window
{
    private readonly ShortcutWindowManager _manager;
    private bool _allowClose;
    private bool _isLeftPressed;
    private bool _dragStarted;
    private Point _dragStartScreenPoint;

    public ShortcutConfig Shortcut { get; }

    public ShortcutWindow(ShortcutWindowManager manager, ShortcutConfig shortcut)
    {
        _manager = manager;
        Shortcut = shortcut;
        InitializeComponent();
        ApplyWindowFromShortcut();
        SetupContextMenu();
        RefreshIcon();
    }

    public void SyncToConfig()
    {
        Shortcut.WindowLeft = Left;
        Shortcut.WindowTop = Top;
        Shortcut.WindowSize = Width;
    }

    /// <summary>仅由管理器调用，用于关闭窗口。</summary>
    public void InternalClose()
    {
        _allowClose = true;
        Close();
    }

    private void ApplyWindowFromShortcut()
    {
        var size = Shortcut.WindowSize <= 0 ? 72 : Shortcut.WindowSize;
        Width = size;
        Height = size;

        var left = NormalizeCoordinate(Shortcut.WindowLeft, 100);
        var top = NormalizeCoordinate(Shortcut.WindowTop, 100);
        Left = left;
        Top = top;
    }

    private static double NormalizeCoordinate(double value, double fallback)
    {
        return double.IsNaN(value) || double.IsInfinity(value) ? fallback : value;
    }

    private void SetupContextMenu()
    {
        var menu = new ContextMenu();

        menu.Items.Add(CreateMenuItem("绑定文件 / 程序", (_, _) => BindFileOrProgram()));
        menu.Items.Add(CreateMenuItem("绑定文件夹", (_, _) => BindFolder()));
        menu.Items.Add(CreateMenuItem("粘贴路径绑定", (_, _) => BindFromClipboard()));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("打开目标所在位置", (_, _) => OpenTargetLocation()));
        menu.Items.Add(CreateMenuItem("复制当前快捷窗口", (_, _) => _manager.DuplicateWindow(this)));
        menu.Items.Add(CreateMenuItem("新建空白快捷窗口", (_, _) => _manager.NewBlankWindow(this)));
        menu.Items.Add(CreateMenuItem("删除当前快捷窗口", (_, _) => _manager.RemoveWindow(this)));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("清除当前绑定", (_, _) => ClearBinding()));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("退出程序", (_, _) => _manager.ExitApplication()));

        ContextMenu = menu;
    }

    private static MenuItem CreateMenuItem(string header, RoutedEventHandler onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += onClick;
        return item;
    }

    private void RefreshIcon()
    {
        IconImage.Source = _manager.ShellIcons.GetIconForPath(Shortcut.TargetPath);
    }

    private void BindFileOrProgram()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择要绑定的文件或程序",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this) == true)
            {
                Shortcut.TargetPath = dialog.FileName;
                RefreshIcon();
                _manager.SaveAll();
            }
        }
        catch (Exception ex)
        {
            ShowError("绑定失败。", ex);
        }
    }

    private void BindFolder()
    {
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择要绑定的文件夹"
            };

            if (dialog.ShowDialog(this) == true)
            {
                Shortcut.TargetPath = dialog.FolderName;
                RefreshIcon();
                _manager.SaveAll();
            }
        }
        catch (Exception ex)
        {
            ShowError("绑定文件夹失败。", ex);
        }
    }

    private void BindFromClipboard()
    {
        try
        {
            if (!Clipboard.ContainsText())
            {
                MessageBox.Show(this, "剪贴板没有可用路径。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var raw = Clipboard.GetText()?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                MessageBox.Show(this, "剪贴板路径为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var path = raw.Trim('"');
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                MessageBox.Show(this, "路径不存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Shortcut.TargetPath = path;
            RefreshIcon();
            _manager.SaveAll();
        }
        catch (Exception ex)
        {
            ShowError("粘贴绑定失败。", ex);
        }
    }

    private void OpenTargetLocation()
    {
        try
        {
            _manager.Launcher.OpenContainingLocation(Shortcut.TargetPath ?? string.Empty);
        }
        catch (Exception ex)
        {
            ShowError("打开目标位置失败。", ex);
        }
    }

    private void ClearBinding()
    {
        try
        {
            Shortcut.TargetPath = null;
            RefreshIcon();
            _manager.SaveAll();
        }
        catch (Exception ex)
        {
            ShowError("清除绑定失败。", ex);
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (e.ClickCount == 2)
            {
                CancelDragTracking();
                _manager.Launcher.Launch(Shortcut.TargetPath ?? string.Empty);
                return;
            }

            _isLeftPressed = true;
            _dragStarted = false;
            _dragStartScreenPoint = PointToScreen(e.GetPosition(this));
            CaptureMouse();
        }
        catch (Exception ex)
        {
            ShowError("打开目标失败。", ex);
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isLeftPressed || _dragStarted || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentPoint = PointToScreen(e.GetPosition(this));
        var deltaX = Math.Abs(currentPoint.X - _dragStartScreenPoint.X);
        var deltaY = Math.Abs(currentPoint.Y - _dragStartScreenPoint.Y);
        if (deltaX < SystemParameters.MinimumHorizontalDragDistance &&
            deltaY < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        try
        {
            _dragStarted = true;
            ReleaseMouseCapture();
            DragMove();
            SyncToConfig();
            _manager.SaveAll();
        }
        catch
        {
            // DragMove 可能在特殊输入状态抛出异常，忽略即可
        }
        finally
        {
            CancelDragTracking();
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        CancelDragTracking();
    }

    private void CancelDragTracking()
    {
        _isLeftPressed = false;
        _dragStarted = false;

        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            return;
        }

        SyncToConfig();
    }

    private void ShowError(string title, Exception ex)
    {
        MessageBox.Show(this, $"{title}\n{ex.Message}", "FloatingShortcut", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
