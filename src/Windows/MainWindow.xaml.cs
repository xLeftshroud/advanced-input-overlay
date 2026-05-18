using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AdvancedInputOverlay.Services;
using AdvancedInputOverlay.ViewModels;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace AdvancedInputOverlay.Windows;

public partial class MainWindow : Window
{
    private OverlayRowViewModel? _dragSource;
    private bool _suppressPersist;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(App.Current.ConfigStore, App.Current.OverlayManager);

        var state = App.Current.ConfigStore.State.MainWindow;
        int initW = state.W > 0 ? state.W : 780;
        int initH = state.H > 0 ? state.H : 500;
        var (x, y, w, h) = ScreenHelper.ClampToVisibleArea(state.X, state.Y, initW, initH);
        _suppressPersist = true;
        Left = x;
        Top = y;
        Width = w;
        Height = h;
        _suppressPersist = false;

        LocationChanged += (_, _) => PersistState();
        SizeChanged += (_, _) => PersistState();
        Closing += OnClosing;
    }

    private MainViewModel Vm => (MainViewModel)DataContext;

    /// <summary>X = hide to tray. Real close happens only when App.IsShuttingDown is set.</summary>
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (App.Current.IsShuttingDown) return;
        e.Cancel = true;
        Hide();
    }

    private void PersistState()
    {
        if (_suppressPersist) return;
        if (WindowState != WindowState.Normal) return;
        var state = App.Current.ConfigStore.State.MainWindow;
        state.X = (int)Left;
        state.Y = (int)Top;
        state.W = (int)ActualWidth;
        state.H = (int)ActualHeight;
        App.Current.ConfigStore.SaveDebounced();
    }

    private void OnDragHandleDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is OverlayRowViewModel vm)
        {
            _dragSource = vm;
            vm.IsDragging = true;
            CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragSource is null) return;
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndDrag();
            return;
        }

        var target = FindRowUnderCursor(e.GetPosition(OverlayList));
        if (target is null || ReferenceEquals(target, _dragSource)) return;

        int from = Vm.Overlays.IndexOf(_dragSource);
        int to = Vm.Overlays.IndexOf(target);
        if (from < 0 || to < 0) return;

        Vm.MoveOverlay(from, to);
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndDrag();
    }

    private void EndDrag()
    {
        if (_dragSource is not null)
        {
            _dragSource.IsDragging = false;
            _dragSource = null;
            ReleaseMouseCapture();
        }
    }

    private OverlayRowViewModel? FindRowUnderCursor(Point posInList)
    {
        var hit = VisualTreeHelper.HitTest(OverlayList, posInList);
        DependencyObject? cur = hit?.VisualHit;
        while (cur is not null)
        {
            if (cur is FrameworkElement fe && fe.DataContext is OverlayRowViewModel row)
                return row;
            cur = VisualTreeHelper.GetParent(cur);
        }
        return null;
    }
}
