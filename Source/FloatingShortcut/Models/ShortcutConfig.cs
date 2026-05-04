namespace FloatingShortcut.Models;

public sealed class ShortcutConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string? TargetPath { get; set; }

    public double WindowLeft { get; set; } = 100;

    public double WindowTop { get; set; } = 100;

    public double WindowSize { get; set; } = 72;
}
