namespace FloatingShortcut.Models;

public sealed class AppConfig
{
    /// <summary>旧版单窗口配置：仅用于读取兼容与迁移。</summary>
    public string? TargetPath { get; set; }

    public double WindowLeft { get; set; } = 100;

    public double WindowTop { get; set; } = 100;

    public double WindowSize { get; set; } = 72;

    public List<ShortcutConfig> Shortcuts { get; set; } = new();

    /// <summary>是否在圆形按钮下方显示绑定目标名称。</summary>
    public bool ShowShortcutNames { get; set; }

    /// <summary>
    /// 确保 <see cref="Shortcuts"/> 至少包含一项。
    /// 若列表为空，则根据旧字段迁移或创建默认快捷项。
    /// </summary>
    public void EnsureShortcuts()
    {
        foreach (var s in Shortcuts)
        {
            if (string.IsNullOrEmpty(s.Id))
            {
                s.Id = Guid.NewGuid().ToString("N");
            }
        }

        if (Shortcuts.Count > 0)
        {
            return;
        }

        var migrated = new ShortcutConfig
        {
            Id = Guid.NewGuid().ToString("N"),
            TargetPath = TargetPath,
            WindowLeft = NormalizeCoordinate(WindowLeft, 100),
            WindowTop = NormalizeCoordinate(WindowTop, 100),
            WindowSize = WindowSize <= 0 ? 72 : WindowSize
        };

        Shortcuts.Add(migrated);
    }

    private static double NormalizeCoordinate(double value, double fallback)
    {
        return double.IsNaN(value) || double.IsInfinity(value) ? fallback : value;
    }
}
