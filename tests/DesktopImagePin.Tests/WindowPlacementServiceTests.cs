using System.Windows;
using DesktopImagePin.Services;

namespace DesktopImagePin.Tests;

public sealed class WindowPlacementServiceTests
{
    private static readonly Rect WorkArea = new(0, 0, 1920, 1040);

    [Fact]
    public void EnsureVisible_PreservesVisibleWindowPosition()
    {
        var position = WindowPlacementService.EnsureVisible(
            new ImageWindowBounds(300, 200, 400, 300),
            [WorkArea]);

        Assert.Equal(new Point(300, 200), position);
    }

    [Fact]
    public void EnsureVisible_MovesOffscreenWindowIntoWorkArea()
    {
        var position = WindowPlacementService.EnsureVisible(
            new ImageWindowBounds(2500, 1500, 400, 300),
            [WorkArea]);

        Assert.Equal(new Point(1520, 740), position);
    }

    [Fact]
    public void BringIntoWorkArea_AlignsOversizedWindowToTopLeft()
    {
        var position = WindowPlacementService.BringIntoWorkArea(
            new ImageWindowBounds(500, 400, 2400, 1400),
            WorkArea);

        Assert.Equal(new Point(0, 0), position);
    }

    [Fact]
    public void EnsureVisible_RescuesWindowFromGapBetweenMonitors()
    {
        Rect[] areas = [new(0, 0, 1920, 1040), new(1920, 1080, 1920, 1040)];
        var bounds = new ImageWindowBounds(2400, 200, 100, 100);

        Assert.False(WindowPlacementService.IsVisible(bounds, areas));
        var position = WindowPlacementService.EnsureVisible(bounds, areas);

        Assert.True(WindowPlacementService.IsVisible(
            bounds with { Left = position.X, Top = position.Y }, areas));
    }

    [Fact]
    public void EnsureVisible_UsesWorkAreaRatherThanTaskbarOrNegativeCoordinateHeuristic()
    {
        Rect[] areas = [new(-1920, 0, 1920, 1040), WorkArea];
        var leftMonitor = new ImageWindowBounds(-1800, 100, 100, 100);
        Assert.Equal(new Point(-1800, 100), WindowPlacementService.EnsureVisible(leftMonitor, areas));

        var behindTaskbar = new ImageWindowBounds(100, 1050, 20, 20);
        Assert.False(WindowPlacementService.IsVisible(behindTaskbar, areas));
        var position = WindowPlacementService.EnsureVisible(behindTaskbar, areas);
        Assert.InRange(position.Y, 0, 1020);
    }

    [Fact]
    public void GroupOffset_PreservesSpacingWhileRescuingAllMembers()
    {
        ImageWindowBounds[] windows = [new(2500, 100, 100, 100), new(2700, 100, 100, 100)];
        var offset = WindowPlacementService.GetGroupOffset(windows, [WorkArea]);
        var moved = windows.Select(window => window with
        {
            Left = window.Left + offset.X,
            Top = window.Top + offset.Y
        }).ToArray();

        Assert.Equal(200, moved[1].Left - moved[0].Left);
        Assert.All(moved, window => Assert.True(WindowPlacementService.IsVisible(window, [WorkArea])));
    }

    [Fact]
    public void OversizedGroup_LeavesAMemberReachableWithoutChangingRelativePositions()
    {
        ImageWindowBounds[] windows = [new(6000, 4000, 100, 100), new(10000, 0, 100, 100)];
        var offset = WindowPlacementService.GetGroupOffset(windows, [WorkArea]);
        Assert.Contains(windows, window => WindowPlacementService.IsVisible(
            window with { Left = window.Left + offset.X, Top = window.Top + offset.Y }, [WorkArea]));
    }

    [Fact]
    public void WorkAreaConversion_UsesSameDpiForNegativePositionsAndSizes()
    {
        var areas = DesktopWorkAreaProvider.ToLogicalWorkAreas(
            [new Rect(-1920, 0, 1920, 1080), new Rect(0, 0, 2880, 1560)], 1.5, 1.5);

        Assert.Equal(new Rect(-1280, 0, 1280, 720), areas[0]);
        Assert.Equal(new Rect(0, 0, 1920, 1040), areas[1]);
    }
}
