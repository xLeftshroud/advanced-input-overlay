using DRect = System.Drawing.Rectangle;
using Screen = System.Windows.Forms.Screen;

namespace AdvancedInputOverlay.Services;

/// <summary>
/// Multi-monitor sanity checks. Saved window positions may point at a screen that no
/// longer exists (laptop undocked, monitor unplugged); without clamping, the window
/// opens off-screen and the user can't drag it back.
/// </summary>
internal static class ScreenHelper
{
    /// <summary>Minimum visible patch we require before we consider a rect "reachable".</summary>
    private const int MinVisible = 80;

    /// <summary>
    /// If the rect has at least <c>MinVisible</c>×<c>MinVisible</c> overlap with any
    /// connected screen's working area, return it unchanged. Otherwise re-center on
    /// the primary screen and clamp size to that screen's working area.
    /// </summary>
    public static (int X, int Y, int W, int H) ClampToVisibleArea(int x, int y, int w, int h)
    {
        // If size is invalid (e.g. caller hasn't decided yet) just clamp position.
        int width = Math.Max(w, 1);
        int height = Math.Max(h, 1);
        var rect = new DRect(x, y, width, height);

        foreach (var screen in Screen.AllScreens)
        {
            var inter = DRect.Intersect(rect, screen.WorkingArea);
            if (inter.Width >= MinVisible && inter.Height >= MinVisible)
                return (x, y, w, h);
        }

        var primary = Screen.PrimaryScreen?.WorkingArea ?? new DRect(0, 0, 1920, 1080);
        int safeW = w > 0 ? Math.Min(w, primary.Width - 80) : 0;
        int safeH = h > 0 ? Math.Min(h, primary.Height - 80) : 0;
        int safeX = primary.X + Math.Max(40, (primary.Width  - (safeW > 0 ? safeW : 400)) / 2);
        int safeY = primary.Y + Math.Max(40, (primary.Height - (safeH > 0 ? safeH : 300)) / 2);
        return (safeX, safeY, safeW, safeH);
    }
}
