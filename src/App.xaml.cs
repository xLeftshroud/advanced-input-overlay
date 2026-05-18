using System.Windows;
using AdvancedInputOverlay.Services;
using AdvancedInputOverlay.Windows;

namespace AdvancedInputOverlay;

public partial class App : System.Windows.Application
{
    // Stable identifiers so a second-launched instance can find the running one.
    private const string MutexName = "Global\\AdvancedInputOverlay-SingleInstance-v1";
    private const string ActivateEventName = "Global\\AdvancedInputOverlay-Activate-v1";

    public ConfigStore ConfigStore { get; } = new();
    public OverlayManager OverlayManager { get; private set; } = null!;
    public InputHook InputHook { get; } = new();
    public bool IsShuttingDown { get; private set; }

    public static new App Current => (App)System.Windows.Application.Current;

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _activateEvent;
    private RegisteredWaitHandle? _activateWait;
    private TrayIcon? _tray;
    private MainWindow? _mainWindow;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        if (!AcquireSingleInstance())
        {
            SignalRunningInstanceAndExit();
            return;
        }

        ConfigStore.Load();
        OverlayManager = new OverlayManager(ConfigStore);

        _mainWindow = new MainWindow();
        MainWindow = _mainWindow;
        _mainWindow.Show();

        OverlayManager.OpenInitiallyVisible();

        InputHook.KeyChanged += OnGlobalKeyChanged;
        InputHook.Start();

        _tray = new TrayIcon(
            tooltip: "Advanced Input Overlay",
            onShow: () => Dispatcher.BeginInvoke(new Action(ShowMainWindow)),
            onExit: () => Dispatcher.BeginInvoke(new Action(ExitApp)));
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        IsShuttingDown = true;
        _activateWait?.Unregister(null);
        _activateEvent?.Dispose();
        _tray?.Dispose();
        InputHook.KeyChanged -= OnGlobalKeyChanged;
        InputHook.Dispose();
        OverlayManager?.CloseAll();
        ConfigStore.Dispose();
        try { _singleInstanceMutex?.ReleaseMutex(); } catch { /* not owner — fine */ }
        _singleInstanceMutex?.Dispose();
    }

    /// <summary>Real app exit (from tray menu or system shutdown).</summary>
    public void ExitApp()
    {
        IsShuttingDown = true;
        Shutdown();
    }

    /// <summary>Reveal the main window from tray / second-instance signal.</summary>
    public void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;
        // Flicker Topmost to force the window to the front of its z-layer.
        _mainWindow.Topmost = true;
        _mainWindow.Topmost = false;
        _mainWindow.Activate();
        _mainWindow.Focus();
    }

    private bool AcquireSingleInstance()
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, name: MutexName, out bool createdNew);
        if (!createdNew)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            return false;
        }

        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
        _activateWait = ThreadPool.RegisterWaitForSingleObject(
            _activateEvent,
            (_, _) => Dispatcher.BeginInvoke(new Action(ShowMainWindow)),
            null,
            Timeout.InfiniteTimeSpan,
            executeOnlyOnce: false);
        return true;
    }

    private void SignalRunningInstanceAndExit()
    {
        try
        {
            if (EventWaitHandle.TryOpenExisting(ActivateEventName, out var handle))
            {
                handle.Set();
                handle.Dispose();
            }
        }
        catch { /* best effort */ }
        Shutdown();
    }

    private void OnGlobalKeyChanged(string key, bool pressed)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            foreach (var win in OverlayManager.ActiveWindows)
                win.SetKeyPressed(key, pressed);
        }));
    }
}
