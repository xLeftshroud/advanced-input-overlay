using AdvancedInputOverlay.Models;
using AdvancedInputOverlay.Windows;

namespace AdvancedInputOverlay.Services;

/// <summary>
/// Lifecycle owner for <see cref="OverlayWindow"/> instances + cross-overlay Z-order
/// application. The list order in <c>ConfigStore.State.Overlays</c> is the source of
/// truth: top of list = highest in z-order within its layer.
/// </summary>
public sealed class OverlayManager
{
    private readonly ConfigStore _store;
    private readonly Dictionary<string, OverlayWindow> _windows = new();

    public event Action<OverlayConfig>? OverlayUserClosed;

    public OverlayManager(ConfigStore store)
    {
        _store = store;
    }

    public IReadOnlyCollection<OverlayWindow> ActiveWindows => _windows.Values;

    public bool TryGet(string id, out OverlayWindow window)
        => _windows.TryGetValue(id, out window!);

    public void OpenInitiallyVisible()
    {
        foreach (var cfg in _store.State.Overlays)
        {
            if (cfg.Visible) OpenInternal(cfg);
        }
        ApplyZOrder();
    }

    public void Open(OverlayConfig cfg)
    {
        if (OpenInternal(cfg)) ApplyZOrder();
    }

    private bool OpenInternal(OverlayConfig cfg)
    {
        if (_windows.ContainsKey(cfg.Id)) return false;
        var win = new OverlayWindow(cfg,
            onChanged: () => _store.SaveDebounced(),
            onUserClose: () => OverlayUserClosed?.Invoke(cfg));
        _windows[cfg.Id] = win;
        win.Show();
        return true;
    }

    public void Close(OverlayConfig cfg)
    {
        if (_windows.TryGetValue(cfg.Id, out var win))
        {
            _windows.Remove(cfg.Id);
            win.CloseForReal();
        }
    }

    public void Reload(OverlayConfig cfg)
    {
        if (_windows.TryGetValue(cfg.Id, out var win))
        {
            win.Title = string.IsNullOrWhiteSpace(cfg.Name) ? "Overlay" : cfg.Name;
            win.TryLoadContent();
        }
    }

    public void ApplyVisible(OverlayConfig cfg)
    {
        if (cfg.Visible) Open(cfg);
        else Close(cfg);
    }

    public void ApplyWindowMode(OverlayConfig cfg)
    {
        if (_windows.TryGetValue(cfg.Id, out var win))
            win.ApplyWindowMode(cfg.WindowMode);
        // Toggling ShowInTaskbar can reset Z-order — re-apply.
        ApplyZOrder();
    }

    public void ApplyTopmost(OverlayConfig cfg)
    {
        // Per-overlay Topmost is meaningful only as input to the cross-overlay z-order pass.
        ApplyZOrder();
    }

    public void ApplyClickThrough(OverlayConfig cfg)
    {
        if (_windows.TryGetValue(cfg.Id, out var win))
            win.ApplyClickThrough(cfg.ClickThrough);
    }

    public void Remove(OverlayConfig cfg) => Close(cfg);

    public void CloseAll()
    {
        var snapshot = _windows.Values.ToList();
        _windows.Clear();
        foreach (var win in snapshot)
        {
            try { win.CloseForReal(); } catch { /* swallow */ }
        }
    }

    /// <summary>
    /// Re-apply window Z-order based on the current list order in
    /// <c>ConfigStore.State.Overlays</c> and each overlay's Topmost flag.
    /// Two-pass algorithm:
    ///   1. Place each window in the topmost or normal layer (HWND_TOPMOST / NOTOPMOST).
    ///   2. Iterate the list bottom→top, bringing each window to the top of its layer.
    /// Net effect: within each layer, list order is preserved; topmost layer always
    /// sits above the normal layer.
    /// </summary>
    public void ApplyZOrder()
    {
        var ordered = _store.State.Overlays;

        // Pass 1: layer assignment
        foreach (var cfg in ordered)
        {
            if (_windows.TryGetValue(cfg.Id, out var win) && win.Hwnd != IntPtr.Zero)
                WindowStyleHelper.SetTopmost(win.Hwnd, cfg.Topmost);
        }

        // Pass 2: within-layer ordering — bottom-to-top so list[0] ends up on top
        for (int i = ordered.Count - 1; i >= 0; i--)
        {
            if (_windows.TryGetValue(ordered[i].Id, out var win) && win.Hwnd != IntPtr.Zero)
                WindowStyleHelper.BringToFrontOfLayer(win.Hwnd);
        }
    }
}
