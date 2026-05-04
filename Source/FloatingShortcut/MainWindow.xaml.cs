using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FloatingShortcut.Models;
using FloatingShortcut.Services;
using Microsoft.Win32;

namespace FloatingShortcut;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService = new();
    private readonly ShellIconService _shellIconService = new();
    private readonly LauncherService _launcherService = new();
    private readonly Stopwatch _dragHoldWatch = new();
    private readonly AppConfig _config;
    private bool _isDragging;

    public MainWindow()
    {
        InitializeComponent();
        _config = _configService.Load();
        ApplyWindowStateFromConfig();
        SetupContextMenu();
        RefreshIcon();
    }

    private void ApplyWindowStateFromConfig()
    {
        var size = _config.WindowSize <= 0 ? 72 : _config.WindowSize;
        Width = size;
        Height = size;

        if (double.IsNaN(_config.WindowLeft) || double.IsInfinity(_config.WindowLeft))
        {
            _config.WindowLeft = 100;
        }

        if (double.IsNaN(_config.WindowTop) || double.IsInfinity(_config.WindowTop))
        {
            _config.WindowTop = 100;
        }

        Left = _config.WindowLeft;
        Top = _config.WindowTop;
    }

    private void SetupContextMenu()
    {
        var menu = new ContextMenu();

        menu.Items.Add(CreateMenuItem("绑定文件 / 程序", (_, _) => BindFileOrProgram()));
        menu.Items.Add(CreateMenuItem("绑定文件夹", (_, _) => BindFolder()));
        menu.Items.Add(CreateMenuItem("粘贴路径绑定", (_, _) => BindFromClipboard()));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("打开目标所在位置", (_, _) => OpenTargetLocation()));
        menu.Items.Add(CreateMenuItem("清除绑定", (_, _) => ClearBinding()));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("退出", (_, _) => Close()));

        ContextMenu = menu;
    }

    private static System.Windows.Controls.MenuItem CreateMenuItem(string header, RoutedEventHandler onClick)
    {
        var item = new System.Windows.Controls.MenuItem { Header = header };
        item.Click += onClick;
        return item;
    }

    private void RefreshIcon()
    {
        IconImage.Source = _shellIconService.GetIconForPath(_config.TargetPath);
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
                _config.TargetPath = dialog.FileName;
                RefreshIcon();
                SaveConfigSafe();
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
                _config.TargetPath = dialog.FolderName;
                RefreshIcon();
                SaveConfigSafe();
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

            _config.TargetPath = path;
            RefreshIcon();
            SaveConfigSafe();
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
            _launcherService.OpenContainingLocation(_config.TargetPath ?? string.Empty);
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
            _config.TargetPath = null;
            RefreshIcon();
            SaveConfigSafe();
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
            if (e.ClickCount != 2)
            {
                return;
            }

            if (e.OriginalSource is DependencyObject source && IsInsideDragHandle(source))
            {
                return;
            }

            _launcherService.Launch(_config.TargetPath ?? string.Empty);
        }
        catch (Exception ex)
        {
            ShowError("打开目标失败。", ex);
        }
    }

    private bool IsInsideDragHandle(DependencyObject source)
    {
        var current = source;
        while (current != null)
        {
            if (ReferenceEquals(current, DragHandle))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void DragHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        _dragHoldWatch.Restart();
        DragHandle.CaptureMouse();
        e.Handled = true;
    }

    private void DragHandle_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (!_dragHoldWatch.IsRunning || _dragHoldWatch.ElapsedMilliseconds < 250)
        {
            return;
        }

        if (_isDragging)
        {
            return;
        }

        try
        {
            _isDragging = true;
            DragMove();
        }
        catch
        {
            // DragMove 可能在特殊输入状态抛出异常，忽略即可
        }
        finally
        {
            _dragHoldWatch.Reset();
            if (DragHandle.IsMouseCaptured)
            {
                DragHandle.ReleaseMouseCapture();
            }

            _isDragging = false;
            SaveConfigSafe();
        }
    }

    private void DragHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragHoldWatch.Reset();
        if (DragHandle.IsMouseCaptured)
        {
            DragHandle.ReleaseMouseCapture();
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveConfigSafe();
    }

    private void SaveConfigSafe()
    {
        try
        {
            _config.WindowLeft = Left;
            _config.WindowTop = Top;
            _config.WindowSize = Width;
            _configService.Save(_config);
        }
        catch (Exception ex)
        {
            ShowError("保存配置失败。", ex);
        }
    }

    private void ShowError(string title, Exception ex)
    {
        MessageBox.Show(this, $"{title}\n{ex.Message}", "FloatingShortcut", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}