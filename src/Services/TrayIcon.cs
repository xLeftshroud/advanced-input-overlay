using System.Drawing;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace AdvancedInputOverlay.Services;

/// <summary>
/// System tray icon with Show / Exit context menu. Wraps WinForms NotifyIcon so we
/// don't have to host a WinForms message loop separately — NotifyIcon is happy
/// running on the WPF main thread.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Action _onShow;
    private readonly Action _onExit;
    private Icon? _appIcon;
    private bool _disposed;

    public TrayIcon(string tooltip, Action onShow, Action onExit)
    {
        _onShow = onShow;
        _onExit = onExit;

        _icon = new NotifyIcon
        {
            Text = tooltip,
            Icon = LoadAppIcon(),
            Visible = true,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) => _onShow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => _onExit());
        _icon.ContextMenuStrip = menu;

        _icon.DoubleClick += (_, _) => _onShow();
    }

    /// <summary>
    /// Load the app icon embedded as a WPF resource (Resources/app.ico). Falls back
    /// to the system Application icon if the resource lookup fails — shouldn't happen
    /// in a normally built exe, but keeps the tray usable in dev / partial builds.
    /// </summary>
    private Icon LoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Resources/app.ico", UriKind.Absolute);
            var info = Application.GetResourceStream(uri);
            if (info != null)
            {
                using var stream = info.Stream;
                _appIcon = new Icon(stream);
                return _appIcon;
            }
        }
        catch
        {
            // fall through to default
        }
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
        _appIcon?.Dispose();
    }
}
