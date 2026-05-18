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
    private bool _disposed;

    public TrayIcon(string tooltip, Action onShow, Action onExit)
    {
        _onShow = onShow;
        _onExit = onExit;

        _icon = new NotifyIcon
        {
            Text = tooltip,
            Icon = SystemIcons.Application,  // TODO M11: bundle a real .ico
            Visible = true,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) => _onShow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => _onExit());
        _icon.ContextMenuStrip = menu;

        _icon.DoubleClick += (_, _) => _onShow();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
    }
}
