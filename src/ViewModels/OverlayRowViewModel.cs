using AdvancedInputOverlay.Models;

namespace AdvancedInputOverlay.ViewModels;

public sealed class OverlayRowViewModel : ObservableObject
{
    private readonly OverlayConfig _config;
    private readonly Action _onChanged;
    private int _index;
    private bool _isDragging;

    public OverlayRowViewModel(OverlayConfig config, Action onChanged)
    {
        _config = config;
        _onChanged = onChanged;
    }

    /// <summary>True only while the user is mouse-down on the drag handle of this row. Drives the lift effect in XAML.</summary>
    public bool IsDragging
    {
        get => _isDragging;
        set => Set(ref _isDragging, value);
    }

    public OverlayConfig Config => _config;

    public int Index
    {
        get => _index;
        set => Set(ref _index, value);
    }

    public string Name
    {
        get => _config.Name;
        set
        {
            if (_config.Name == value) return;
            _config.Name = value;
            OnPropertyChanged();
            _onChanged();
        }
    }

    public bool Visible
    {
        get => _config.Visible;
        set
        {
            if (_config.Visible == value) return;
            _config.Visible = value;
            OnPropertyChanged();
            _onChanged();
        }
    }

    public bool WindowMode
    {
        get => _config.WindowMode;
        set
        {
            if (_config.WindowMode == value) return;
            _config.WindowMode = value;
            OnPropertyChanged();
            _onChanged();
        }
    }

    public bool Topmost
    {
        get => _config.Topmost;
        set
        {
            if (_config.Topmost == value) return;
            _config.Topmost = value;
            OnPropertyChanged();
            _onChanged();
        }
    }

    public bool ClickThrough
    {
        get => _config.ClickThrough;
        set
        {
            if (_config.ClickThrough == value) return;
            _config.ClickThrough = value;
            OnPropertyChanged();
            _onChanged();
        }
    }

    /// <summary>Re-fire PropertyChanged for fields that may have been updated underneath us (e.g., by edit modal).</summary>
    public void NotifyAllChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Visible));
        OnPropertyChanged(nameof(WindowMode));
        OnPropertyChanged(nameof(Topmost));
        OnPropertyChanged(nameof(ClickThrough));
    }
}
