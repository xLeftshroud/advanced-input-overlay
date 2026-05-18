using System.Text.Json.Serialization;

namespace AdvancedInputOverlay.Models;

/// <summary>
/// Root JSON shape that the user provides for an overlay layout.
/// </summary>
public sealed class LayoutSchema
{
    /// <summary>Suggested initial width of the overlay window. 0 = derive from bounding box of elements.</summary>
    public int Width { get; set; }

    /// <summary>Suggested initial height of the overlay window. 0 = derive from bounding box.</summary>
    public int Height { get; set; }

    public List<LayoutElement> Elements { get; set; } = new();
}

public sealed class LayoutElement
{
    /// <summary>"texture" | "key" | "mouse"</summary>
    public string Type { get; set; } = "texture";

    /// <summary>Key name (e.g. "W", "LShift", "MouseLeft"). Only used for type == "key" | "mouse".</summary>
    public string? Key { get; set; }

    /// <summary>Sprite rect on the source PNG for the normal (not pressed) state.</summary>
    public SrcRect Src { get; set; } = new();

    /// <summary>
    /// Sprite rect on the source PNG for the pressed state.
    /// If null and Type is "key" or "mouse", defaults to <see cref="EffectivePressedSrc"/>.
    /// </summary>
    public SrcRect? PressedSrc { get; set; }

    /// <summary>Top-left position on the overlay window.</summary>
    public PosPoint Pos { get; set; } = new();

    /// <summary>Convenience: returns explicit pressed_src or the default offset (y + h + 3).</summary>
    [JsonIgnore]
    public SrcRect EffectivePressedSrc => PressedSrc ?? new SrcRect
    {
        X = Src.X,
        Y = Src.Y + Src.H + 3,
        W = Src.W,
        H = Src.H,
    };
}

public sealed class SrcRect
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
}

public sealed class PosPoint
{
    public int X { get; set; }
    public int Y { get; set; }
}
