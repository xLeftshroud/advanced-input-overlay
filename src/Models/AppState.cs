namespace AdvancedInputOverlay.Models;

public sealed class AppState
{
    public WindowRect MainWindow { get; set; } = new() { X = 100, Y = 100, W = 800, H = 500 };

    public List<OverlayConfig> Overlays { get; set; } = new();
}

public sealed class WindowRect
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
}
