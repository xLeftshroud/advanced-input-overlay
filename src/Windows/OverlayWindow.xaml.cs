using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AdvancedInputOverlay.Models;
using AdvancedInputOverlay.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace AdvancedInputOverlay.Windows;

/// <summary>
/// One transparent overlay window driven by an <see cref="OverlayConfig"/>.
///
/// Window Mode toggling expands/contracts the OUTER window dimensions by the chrome
/// size so the inner content stays at the exact same screen position. Content scaling
/// is delegated to a <see cref="System.Windows.Controls.Viewbox"/> with
/// Stretch=Uniform — user resizes window → content scales proportionally inside.
/// </summary>
public partial class OverlayWindow : Window
{
    // Visual chrome dimensions for Window Mode ON.
    private const double CaptionHeightPx = 28;
    private const double SideBorderPx = 4;
    private const double BottomBorderPx = 4;

    private static double TotalChromeWidth() => 2 * SideBorderPx;                       // 8
    private static double TotalChromeHeight() => CaptionHeightPx + BottomBorderPx;      // 32

    private readonly OverlayConfig _config;
    private readonly Action _onChanged;
    private readonly Action _onUserClose;
    private IntPtr _hwnd;
    private bool _loaded;
    private bool _allowClose;
    private bool _suppressPersist;
    private bool _chromeApplied;  // tracks visual state — config.WindowMode may already match before we grow

    public OverlayConfig Config => _config;
    public IntPtr Hwnd => _hwnd;

    public OverlayWindow(OverlayConfig config, Action onChanged, Action onUserClose)
    {
        _config = config;
        _onChanged = onChanged;
        _onUserClose = onUserClose;

        InitializeComponent();

        Title = string.IsNullOrWhiteSpace(_config.Name) ? "Overlay" : _config.Name;
        TitleText.Text = Title;

        int wantW = _config.Window.W > 0 ? _config.Window.W : 400;
        int wantH = _config.Window.H > 0 ? _config.Window.H : 300;
        var (sx, sy, sw, sh) = ScreenHelper.ClampToVisibleArea(_config.Window.X, _config.Window.Y, wantW, wantH);
        Left = sx;
        Top = sy;
        Width = sw;
        Height = sh;

        SourceInitialized += OnSourceInitialized;
        LocationChanged += OnLocationChanged;
        SizeChanged += OnSizeChanged;
        Closing += OnClosing;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = WindowStyleHelper.GetHwnd(this);

        // AllowsTransparency=true already set WS_EX_LAYERED. We layer in WS_EX_TRANSPARENT
        // only when click-through is requested.
        WindowStyleHelper.SetClickThrough(_hwnd, _config.ClickThrough);
        WindowStyleHelper.SetTopmost(_hwnd, _config.Topmost);

        // Taskbar icon (and Alt-Tab) only when Window Mode is on — the borderless
        // "decoration" form should be invisible to the OS shell. WPF's
        // ShowInTaskbar setter toggles WS_EX_TOOLWINDOW for us.
        ShowInTaskbar = _config.WindowMode;

        // Visual chrome state — no geometric growth at this point: stored W/H already
        // include or exclude chrome based on the mode that was active when saved.
        ApplyChromeVisuals(_config.WindowMode);
        _chromeApplied = _config.WindowMode;
        TryLoadContent();
    }

    public void TryLoadContent()
    {
        if (_loaded) Canvas.ClearContent();
        try
        {
            Canvas.Load(_config.ImagePath, _config.LayoutPath);
            _loaded = true;

            // First time we ever see this overlay (no saved size yet) → size to fit content
            // plus chrome if currently in Window Mode.
            if (_config.Window.W <= 0 || _config.Window.H <= 0)
            {
                _suppressPersist = true;
                Width = Canvas.NaturalSize.Width + (_config.WindowMode ? TotalChromeWidth() : 0);
                Height = Canvas.NaturalSize.Height + (_config.WindowMode ? TotalChromeHeight() : 0);
                _suppressPersist = false;
                PersistGeometry();
            }
        }
        catch (Exception ex)
        {
            _loaded = false;
            System.Diagnostics.Debug.WriteLine($"[OverlayWindow] Failed to load {_config.Name}: {ex.Message}");
            // Surface to user — defer so we don't block SourceInitialized.
            var name = string.IsNullOrWhiteSpace(_config.Name) ? "Overlay" : _config.Name;
            var msg = $"Failed to load overlay \"{name}\":\n\n{ex.Message}\n\nClick Edit on the row in the main window to update the image / config paths.";
            Dispatcher.BeginInvoke(new Action(() =>
                MessageBox.Show(this, msg, "Overlay load error", MessageBoxButton.OK, MessageBoxImage.Warning)));
        }
    }

    public void ApplyWindowMode(bool windowMode)
    {
        _config.WindowMode = windowMode;
        // VM's setter already updated _config.WindowMode before calling us, so guard
        // against double-grow by comparing to the visual state we last applied.
        if (_chromeApplied == windowMode) return;

        ShowInTaskbar = windowMode;
        _suppressPersist = true;
        double dx = SideBorderPx;
        double dy = CaptionHeightPx;
        double dw = TotalChromeWidth();
        double dh = TotalChromeHeight();
        if (windowMode)
        {
            Left -= dx;
            Top -= dy;
            Width += dw;
            Height += dh;
        }
        else
        {
            Left += dx;
            Top += dy;
            Width = Math.Max(1, Width - dw);
            Height = Math.Max(1, Height - dh);
        }
        ApplyChromeVisuals(windowMode);
        _chromeApplied = windowMode;
        _suppressPersist = false;
        PersistGeometry();
    }

    private void ApplyChromeVisuals(bool windowMode)
    {
        if (windowMode)
        {
            TitleBar.Visibility = Visibility.Visible;
            TitleRow.Height = new GridLength(CaptionHeightPx);
            ContentBorder.BorderThickness = new Thickness(SideBorderPx, 0, SideBorderPx, BottomBorderPx);
            Chrome.CaptionHeight = CaptionHeightPx;
            Chrome.ResizeBorderThickness = new Thickness(SideBorderPx);
        }
        else
        {
            TitleBar.Visibility = Visibility.Collapsed;
            TitleRow.Height = new GridLength(0);
            ContentBorder.BorderThickness = new Thickness(0);
            Chrome.CaptionHeight = 0;
            Chrome.ResizeBorderThickness = new Thickness(0);
        }
    }

    public void ApplyClickThrough(bool clickThrough)
    {
        _config.ClickThrough = clickThrough;
        if (_hwnd != IntPtr.Zero)
            WindowStyleHelper.SetClickThrough(_hwnd, clickThrough);
    }

    public void ApplyTopmost(bool topmost)
    {
        _config.Topmost = topmost;
        if (_hwnd != IntPtr.Zero)
            WindowStyleHelper.SetTopmost(_hwnd, topmost);
    }

    public void SetKeyPressed(string key, bool pressed)
    {
        if (!_loaded) return;
        Canvas.SetKeyPressed(key, pressed);
    }

    public void CloseForReal()
    {
        _allowClose = true;
        Close();
    }

    private void PersistGeometry()
    {
        if (_suppressPersist) return;
        if (WindowState != WindowState.Normal) return;
        _config.Window.X = (int)Left;
        _config.Window.Y = (int)Top;
        _config.Window.W = (int)ActualWidth;
        _config.Window.H = (int)ActualHeight;
        _onChanged();
    }

    private void OnLocationChanged(object? sender, EventArgs e) => PersistGeometry();
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => PersistGeometry();

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        if (App.Current.IsShuttingDown) return;
        // User clicked the title-bar X or right-click → Close in taskbar. Cancel the
        // native close and defer the visibility sync to the next dispatcher tick so we
        // don't re-enter Close() while still inside WmClose.
        e.Cancel = true;
        Dispatcher.BeginInvoke(_onUserClose);
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_config.WindowMode) return;       // native caption handles drag
        if (_config.ClickThrough) return;     // event won't fire anyway
        try { DragMove(); }
        catch { /* mouse already up */ }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        // Same deferral as OnClosing — this button is hit-test visible while we're
        // in the middle of WPF input handling.
        Dispatcher.BeginInvoke(_onUserClose);
    }
}
