using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using Vector = System.Windows.Vector;

namespace DesktopImagePin.Services;

public readonly record struct ImageWindowBounds(
    double Left,
    double Top,
    double Width,
    double Height);

public static class WindowPlacementService
{
    private const double MinimumVisibleSize = 48;

    public static bool IsVisible(ImageWindowBounds window, IReadOnlyList<Rect> workAreas)
    {
        return workAreas.Any(area => IsVisible(window, area));
    }

    public static bool IsVisible(ImageWindowBounds window, Rect visibleBounds)
    {
        var intersection = Rect.Intersect(
            new Rect(window.Left, window.Top, Math.Max(1, window.Width), Math.Max(1, window.Height)),
            visibleBounds);

        return !intersection.IsEmpty
            && intersection.Width >= Math.Min(MinimumVisibleSize, window.Width)
            && intersection.Height >= Math.Min(MinimumVisibleSize, window.Height);
    }

    public static Point EnsureVisible(
        ImageWindowBounds window,
        IReadOnlyList<Rect> workAreas)
    {
        var offset = GetGroupOffset([window], workAreas);
        return new Point(window.Left + offset.X, window.Top + offset.Y);
    }

    public static Vector GetGroupOffset(
        IReadOnlyList<ImageWindowBounds> windows,
        IReadOnlyList<Rect> workAreas)
    {
        if (windows.Count == 0 || workAreas.Count == 0
            || windows.All(window => IsVisible(window, workAreas)))
        {
            return new Vector();
        }

        var groupRect = Rect.Empty;
        foreach (var window in windows)
        {
            groupRect.Union(new Rect(window.Left, window.Top, window.Width, window.Height));
        }

        var groupBounds = new ImageWindowBounds(groupRect.Left, groupRect.Top, groupRect.Width, groupRect.Height);
        var candidates = new List<Vector>();
        foreach (var area in workAreas)
        {
            var position = BringIntoWorkArea(groupBounds, area);
            candidates.Add(position - groupRect.TopLeft);

            // A group larger than a monitor must still leave a member reachable.
            if (groupRect.Width > area.Width || groupRect.Height > area.Height)
            {
                foreach (var window in windows)
                {
                    candidates.Add(BringIntoWorkArea(window, area) - new Point(window.Left, window.Top));
                }
            }
        }

        return candidates
            .OrderByDescending(offset => windows.Count(window => IsVisible(
                window with { Left = window.Left + offset.X, Top = window.Top + offset.Y }, workAreas)))
            .ThenBy(offset => offset.LengthSquared)
            .First();
    }

    public static Point BringIntoWorkArea(ImageWindowBounds window, Rect workArea)
    {
        var maximumLeft = window.Width <= workArea.Width
            ? workArea.Right - window.Width
            : workArea.Left;
        var maximumTop = window.Height <= workArea.Height
            ? workArea.Bottom - window.Height
            : workArea.Top;

        return new Point(
            Math.Clamp(window.Left, workArea.Left, maximumLeft),
            Math.Clamp(window.Top, workArea.Top, maximumTop));
    }
}
