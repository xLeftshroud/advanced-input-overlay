namespace AdvancedInputOverlay.Models;

public sealed class OverlayConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    public string ImagePath { get; set; } = "";

    public string LayoutPath { get; set; } = "";

    public bool Visible { get; set; } = true;

    public bool WindowMode { get; set; } = false;

    public bool Topmost { get; set; } = true;

    public bool ClickThrough { get; set; } = false;

    /// <summary>Position / size on screen. W=0 or H=0 means "auto-fit to layout natural size on first load".</summary>
    public WindowRect Window { get; set; } = new() { X = 200, Y = 200, W = 0, H = 0 };
}
