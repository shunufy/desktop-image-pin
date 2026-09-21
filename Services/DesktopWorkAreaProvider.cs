using System.Windows;
using Forms = System.Windows.Forms;

namespace DesktopImagePin.Services;

internal static class DesktopWorkAreaProvider
{
    public static IReadOnlyList<Rect> GetWorkAreas()
    {
        var logicalPrimary = SystemParameters.WorkArea;
        var primary = Forms.Screen.PrimaryScreen;
        if (primary is null || primary.WorkingArea.Width <= 0 || primary.WorkingArea.Height <= 0)
        {
            return [logicalPrimary];
        }

        // Screen uses pixels; WPF positions use the application's system-DPI units.
        return ToLogicalWorkAreas(
            Forms.Screen.AllScreens.Select(screen => new Rect(
                screen.WorkingArea.Left,
                screen.WorkingArea.Top,
                screen.WorkingArea.Width,
                screen.WorkingArea.Height)),
            primary.WorkingArea.Width / logicalPrimary.Width,
            primary.WorkingArea.Height / logicalPrimary.Height);
    }

    internal static IReadOnlyList<Rect> ToLogicalWorkAreas(
        IEnumerable<Rect> pixelAreas, double dpiScaleX, double dpiScaleY)
    {
        return pixelAreas.Select(area => new Rect(
            area.Left / dpiScaleX,
            area.Top / dpiScaleY,
            area.Width / dpiScaleX,
            area.Height / dpiScaleY)).ToArray();
    }
}
