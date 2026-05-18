using System.Collections.ObjectModel;
using System.Windows.Input;
using AdvancedInputOverlay.Models;
using AdvancedInputOverlay.Services;
using AdvancedInputOverlay.Windows;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace AdvancedInputOverlay.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ConfigStore _store;
    private readonly OverlayManager _manager;

    public ObservableCollection<OverlayRowViewModel> Overlays { get; } = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public MainViewModel(ConfigStore store, OverlayManager manager)
    {
        _store = store;
        _manager = manager;
        AddCommand = new RelayCommand(_ => OpenAddOverlay(null));
        EditCommand = new RelayCommand(p => OpenAddOverlay(p as OverlayRowViewModel));
        DeleteCommand = new RelayCommand(p => Delete(p as OverlayRowViewModel));

        _manager.OverlayUserClosed += OnOverlayUserClosed;

        InitFromStore();
    }

    private void OnOverlayUserClosed(OverlayConfig cfg)
    {
        // User clicked X on a Window-Mode-ON overlay window. Reflect in the row's S toggle.
        var row = Overlays.FirstOrDefault(r => r.Config.Id == cfg.Id);
        if (row != null && row.Visible)
        {
            row.Visible = false;
        }
    }

    private void InitFromStore()
    {
        for (int i = 0; i < _store.State.Overlays.Count; i++)
        {
            var cfg = _store.State.Overlays[i];
            var vm = MakeRow(cfg);
            vm.Index = i + 1;
            Overlays.Add(vm);
        }
    }

    private OverlayRowViewModel MakeRow(OverlayConfig cfg)
    {
        var vm = new OverlayRowViewModel(cfg, () => _store.SaveDebounced());
        // Listen for toggle changes so we can drive OverlayManager.
        vm.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(OverlayRowViewModel.Visible):
                    _manager.ApplyVisible(cfg);
                    break;
                case nameof(OverlayRowViewModel.WindowMode):
                    _manager.ApplyWindowMode(cfg);
                    break;
                case nameof(OverlayRowViewModel.Topmost):
                    _manager.ApplyTopmost(cfg);
                    break;
                case nameof(OverlayRowViewModel.ClickThrough):
                    _manager.ApplyClickThrough(cfg);
                    break;
            }
        };
        return vm;
    }

    private void OpenAddOverlay(OverlayRowViewModel? existing)
    {
        var dialog = new AddOverlayWindow
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        if (existing != null)
        {
            dialog.LoadFrom(existing.Config);
        }

        if (dialog.ShowDialog() == true)
        {
            if (existing != null)
            {
                existing.Config.Name = dialog.OverlayName;
                existing.Config.ImagePath = dialog.ImagePath;
                existing.Config.LayoutPath = dialog.LayoutPath;
                existing.NotifyAllChanged();
                _manager.Reload(existing.Config);
                _store.SaveDebounced();
            }
            else
            {
                var cfg = new OverlayConfig
                {
                    Name = dialog.OverlayName,
                    ImagePath = dialog.ImagePath,
                    LayoutPath = dialog.LayoutPath,
                };
                _store.State.Overlays.Add(cfg);
                var vm = MakeRow(cfg);
                vm.Index = Overlays.Count + 1;
                Overlays.Add(vm);
                if (cfg.Visible) _manager.Open(cfg);
                _store.SaveDebounced();
            }
        }
    }

    private void Delete(OverlayRowViewModel? row)
    {
        if (row is null) return;

        var name = string.IsNullOrWhiteSpace(row.Name) ? "this overlay" : $"\"{row.Name}\"";
        var result = MessageBox.Show(
            $"Delete {name}?\n\nThis only removes it from the list — your image and JSON files are not touched.",
            "Delete overlay",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Cancel);
        if (result != MessageBoxResult.OK) return;

        _manager.Remove(row.Config);
        Overlays.Remove(row);
        _store.State.Overlays.Remove(row.Config);
        Reindex();
        _store.SaveDebounced();
    }

    private void Reindex()
    {
        for (int i = 0; i < Overlays.Count; i++)
        {
            Overlays[i].Index = i + 1;
        }
    }

    /// <summary>
    /// Reorder a row in the list (called by the drag-handle interaction in MainWindow).
    /// Also moves the backing OverlayConfig in <c>ConfigStore.State.Overlays</c>
    /// so persisted order matches, then re-applies cross-overlay Z-order.
    /// </summary>
    public void MoveOverlay(int from, int to)
    {
        if (from < 0 || to < 0) return;
        if (from >= Overlays.Count || to >= Overlays.Count) return;
        if (from == to) return;

        Overlays.Move(from, to);

        var cfgs = _store.State.Overlays;
        var cfg = cfgs[from];
        cfgs.RemoveAt(from);
        cfgs.Insert(to, cfg);

        Reindex();
        _manager.ApplyZOrder();
        _store.SaveDebounced();
    }
}
