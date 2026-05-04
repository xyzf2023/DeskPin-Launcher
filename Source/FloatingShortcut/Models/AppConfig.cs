namespace FloatingShortcut.Models;

public sealed class AppConfig
{
    public string? TargetPath { get; set; }

    public double WindowLeft { get; set; } = 100;

    public double WindowTop { get; set; } = 100;

    public double WindowSize { get; set; } = 72;
}
