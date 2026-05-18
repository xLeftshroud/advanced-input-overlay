using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AdvancedInputOverlay.Services;

/// <summary>
/// P/Invoke helpers for overlay window styling: click-through (WS_EX_TRANSPARENT),
/// tool window flag, topmost / Z-order. Window Mode chrome is handled by
/// <see cref="System.Windows.Shell.WindowChrome"/> in XAML (per-pixel alpha requires
/// AllowsTransparency=true, which forbids dynamic native chrome via Win32).
/// </summary>
internal static class WindowStyleHelper
{
    private const int GWL_EXSTYLE = -20;

    private const uint WS_EX_LAYERED = 0x00080000;
    private const uint WS_EX_TRANSPARENT = 0x00000020;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new(-2);
    private static readonly IntPtr HWND_TOP = new(0);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        => IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong32(hWnd, nIndex));

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        => IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));

    public static IntPtr GetHwnd(Window window) => new WindowInteropHelper(window).Handle;

    /// <summary>Add or remove WS_EX_TRANSPARENT. WS_EX_LAYERED is already on (set by AllowsTransparency=true).</summary>
    public static void SetClickThrough(IntPtr hwnd, bool clickThrough)
    {
        long ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        ex |= WS_EX_LAYERED;
        if (clickThrough)
            ex |= WS_EX_TRANSPARENT;
        else
            ex &= ~WS_EX_TRANSPARENT;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex));
    }

    /// <summary>Hide from taskbar / Alt-Tab via WS_EX_TOOLWINDOW.</summary>
    public static void SetToolWindow(IntPtr hwnd, bool toolWindow)
    {
        long ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        if (toolWindow)
            ex |= WS_EX_TOOLWINDOW;
        else
            ex &= ~WS_EX_TOOLWINDOW;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex));
    }

    /// <summary>Move into the topmost or normal Z layer.</summary>
    public static void SetTopmost(IntPtr hwnd, bool topmost)
    {
        SetWindowPos(hwnd, topmost ? HWND_TOPMOST : HWND_NOTOPMOST,
            0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    /// <summary>Bring the window to the top of its current Z layer (for M7 ordering).</summary>
    public static void BringToFrontOfLayer(IntPtr hwnd)
    {
        SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }
}
