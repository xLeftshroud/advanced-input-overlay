using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AdvancedInputOverlay.Models;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;
using Size = System.Windows.Size;

namespace AdvancedInputOverlay.Controls;

/// <summary>
/// Renders a <see cref="LayoutSchema"/> against a source PNG. Each element becomes an
/// <see cref="Image"/> child whose Source is a <see cref="CroppedBitmap"/> of the parent
/// bitmap. Pressed/release state is updated via <see cref="SetKeyPressed"/>.
/// </summary>
public sealed class OverlayCanvas : Canvas
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private BitmapSource? _spriteSheet;
    private LayoutSchema? _layout;

    private readonly Dictionary<string, List<ElementVisual>> _byKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _pressed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Computed natural size from layout (Width/Height if set, else bounding box).</summary>
    public Size NaturalSize { get; private set; } = new(400, 300);

    public OverlayCanvas()
    {
        Background = Brushes.Transparent;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
    }

    /// <summary>
    /// Load image + layout from disk and rebuild the visual tree.
    /// Throws on I/O / parse failure; caller surfaces error to user.
    /// </summary>
    public void Load(string imagePath, string layoutPath)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Overlay image not found.", imagePath);
        if (!File.Exists(layoutPath))
            throw new FileNotFoundException("Overlay layout (json) not found.", layoutPath);

        // Load image with OnLoad caching so we don't lock the file.
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        bmp.UriSource = new Uri(imagePath, UriKind.Absolute);
        bmp.EndInit();
        bmp.Freeze();
        _spriteSheet = bmp;

        // Load layout
        var json = File.ReadAllText(layoutPath);
        var layout = JsonSerializer.Deserialize<LayoutSchema>(json, JsonOpts)
                     ?? throw new InvalidDataException("Layout file is empty or invalid.");
        _layout = layout;

        Rebuild();
    }

    public void ClearContent()
    {
        Children.Clear();
        _byKey.Clear();
        _spriteSheet = null;
        _layout = null;
    }

    /// <summary>Mark a key (e.g. "W", "MouseLeft") as pressed and refresh affected sprites.</summary>
    public void SetKeyPressed(string key, bool pressed)
    {
        bool changed = pressed ? _pressed.Add(key) : _pressed.Remove(key);
        if (!changed) return;
        if (_byKey.TryGetValue(key, out var visuals))
        {
            foreach (var v in visuals)
                v.SetPressed(pressed);
        }
    }

    private void Rebuild()
    {
        Children.Clear();
        _byKey.Clear();

        if (_spriteSheet is null || _layout is null) return;

        int maxRight = 0, maxBottom = 0;

        foreach (var el in _layout.Elements)
        {
            var visual = new ElementVisual(el, _spriteSheet);
            SetLeft(visual.Image, el.Pos.X);
            SetTop(visual.Image, el.Pos.Y);
            Children.Add(visual.Image);

            // For type=key/mouse, register by key so input events can flip its source.
            if ((el.Type == "key" || el.Type == "mouse") && !string.IsNullOrEmpty(el.Key))
            {
                if (!_byKey.TryGetValue(el.Key, out var list))
                {
                    list = new List<ElementVisual>();
                    _byKey[el.Key] = list;
                }
                list.Add(visual);

                // Restore visual state if the key is currently held when we (re)load.
                if (_pressed.Contains(el.Key))
                    visual.SetPressed(true);
            }

            int right = el.Pos.X + el.Src.W;
            int bottom = el.Pos.Y + el.Src.H;
            if (right > maxRight) maxRight = right;
            if (bottom > maxBottom) maxBottom = bottom;
        }

        // Natural size: explicit if provided, else bounding box.
        int w = _layout.Width > 0 ? _layout.Width : maxRight;
        int h = _layout.Height > 0 ? _layout.Height : maxBottom;
        NaturalSize = new Size(Math.Max(w, 1), Math.Max(h, 1));

        Width = NaturalSize.Width;
        Height = NaturalSize.Height;
    }

    /// <summary>One renderable element. Holds Image control + cached cropped bitmaps.</summary>
    private sealed class ElementVisual
    {
        public Image Image { get; }
        private readonly BitmapSource _normalCrop;
        private readonly BitmapSource? _pressedCrop;

        public ElementVisual(LayoutElement el, BitmapSource sheet)
        {
            _normalCrop = Crop(sheet, el.Src);
            if (el.Type == "key" || el.Type == "mouse")
            {
                _pressedCrop = Crop(sheet, el.EffectivePressedSrc);
            }

            Image = new Image
            {
                Source = _normalCrop,
                Width = el.Src.W,
                Height = el.Src.H,
                Stretch = Stretch.Fill,
                SnapsToDevicePixels = true,
            };
            RenderOptions.SetBitmapScalingMode(Image, BitmapScalingMode.HighQuality);
        }

        public void SetPressed(bool pressed)
        {
            Image.Source = (pressed && _pressedCrop is not null) ? _pressedCrop : _normalCrop;
        }

        private static BitmapSource Crop(BitmapSource source, SrcRect rect)
        {
            // Clamp rect into bounds to avoid CroppedBitmap throwing on bad layouts.
            int x = Math.Max(0, Math.Min(rect.X, source.PixelWidth - 1));
            int y = Math.Max(0, Math.Min(rect.Y, source.PixelHeight - 1));
            int w = Math.Max(1, Math.Min(rect.W, source.PixelWidth - x));
            int h = Math.Max(1, Math.Min(rect.H, source.PixelHeight - y));

            var cropped = new CroppedBitmap(source, new Int32Rect(x, y, w, h));
            cropped.Freeze();
            return cropped;
        }
    }
}
