using System.Windows;
using DesktopImagePin.Models;
using DesktopImagePin.Services;

namespace DesktopImagePin.Tests;

public sealed class ImageManagerRecoveryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"DesktopImagePin.RecoveryTests-{Guid.NewGuid():N}");
    private readonly string _imagePath;
    private static readonly Rect WorkArea = new(0, 0, 1920, 1040);

    public ImageManagerRecoveryTests()
    {
        Directory.CreateDirectory(_directory);
        _imagePath = Path.Combine(_directory, "sample.png");
        WpfTestHelper.SaveImage(_imagePath);
    }

    private static ImageManager CreateManager() => new(() => [WorkArea], _ => { });

    [Fact]
    public void RealImageWindows_ShowAndCloseWithCorrectRegistrationCounts()
    {
        WpfTestHelper.Run(() =>
        {
            var manager = new ImageManager(DesktopWorkAreaProvider.GetWorkAreas, window =>
            {
                window.ShowActivated = false;
                window.Show();
            });
            try
            {
                var first = manager.AddImage(_imagePath, new() { Left = 100, Top = 100 });
                var second = manager.AddImage(_imagePath, new() { Left = 250, Top = 100 });
                Assert.True(first.Window!.IsVisible);
                Assert.True(second.Window!.IsVisible);
                Assert.Equal(2, manager.ImageCount);
                Assert.NotEqual(IntPtr.Zero, new System.Windows.Interop.WindowInteropHelper(first.Window).Handle);
                manager.GroupImages([first, second]);
                var originalSpacing = second.Window.Left - first.Window.Left;
                manager.ScaleImageOrGroup(first, 1.1, 1.1);
                var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(first.Window);
                Assert.InRange(Math.Abs(second.Window.Left - first.Window.Left - originalSpacing * 1.1),
                    0, 2 / dpi.DpiScaleX);
                second.Window.Close();
                Assert.Same(first, Assert.Single(manager.Items));
                Assert.False(first.IsGrouped);
                Assert.Equal(1, manager.ImageCount);
            }
            finally
            {
                manager.RemoveAll();
            }

            Assert.Equal(0, manager.ImageCount);
            Assert.Equal(0, manager.UnavailableCount);
        });
    }

    [Fact]
    public void FailedWindowDisplay_RemainsAvailableForRetry()
    {
        WpfTestHelper.Run(() =>
        {
            var manager = new ImageManager(() => [WorkArea], _ => throw new InvalidOperationException("test display failure"));
            manager.RestoreImages([new() { FilePath = _imagePath, Left = 200 }]);
            var item = Assert.Single(manager.Items);
            Assert.True(item.IsUnavailable);
            Assert.Throws<InvalidOperationException>(() => manager.RetryImage(item));
            Assert.Same(item, Assert.Single(manager.Items));
            Assert.Equal(200, item.Left);
        });
    }

    [Fact]
    public void MissingAndCorruptImages_RetainSettingsAndGroupAcrossRepeatedSaves()
    {
        WpfTestHelper.Run(() =>
        {
            var groupId = Guid.NewGuid();
            var missingPath = Path.Combine(_directory, "missing.png");
            var corruptPath = Path.Combine(_directory, "corrupt.png");
            File.WriteAllText(corruptPath, "not an image");
            var manager = CreateManager();
            manager.RestoreImages([
                new() { FilePath = missingPath, GroupId = groupId, Left = 120, Top = 200,
                    Scale = 0.5, ScaleX = 0.75, ScaleY = 0.5, RotationDegrees = 90,
                    DisplayLayer = ImageDisplayLayer.Topmost, IsClickThrough = true, FlipHorizontal = true, Opacity = 0.7 },
                new() { FilePath = corruptPath, GroupId = groupId, Left = 400, Top = 500 }
            ]);

            Assert.Equal(2, manager.UnavailableCount);
            Assert.Equal(0, manager.ImageCount);
            var store = new ImageStateStore(Path.Combine(_directory, "images.json"));
            store.Save(manager.Items);
            store.Save(manager.Items);
            var saved = store.Load();
            Assert.Equal(2, saved.Count);
            Assert.All(saved, state => Assert.Equal(groupId, state.GroupId));
            var missing = Assert.Single(saved, state => state.FilePath == missingPath);
            Assert.Equal(120, missing.Left);
            Assert.Equal(200, missing.Top);
            Assert.Equal(0.75, missing.ScaleX);
            Assert.Equal(0.5, missing.ScaleY);
            Assert.Equal(90, missing.RotationDegrees);
            Assert.True(missing.IsClickThrough);
            Assert.True(missing.FlipHorizontal);
            Assert.Equal(0.7, missing.Opacity);
            Assert.Equal(ImageDisplayLayer.Topmost, missing.DisplayLayer);
            Assert.Equal(2, new ImageStateStore(Path.Combine(_directory, "images.backup.json")).Load().Count);

            File.Copy(_imagePath, corruptPath, overwrite: true);
            manager.RetryImage(manager.Items[1]);
            Assert.False(manager.Items[1].IsUnavailable);

            manager.RemoveImage(manager.Items[0]);
            store.Save(manager.Items);
            Assert.Single(store.Load());
            manager.RemoveAll();
        });
    }

    [Fact]
    public void Restoration_DoesNotRequestAutosaveButSubsequentEditsDo()
    {
        WpfTestHelper.Run(() =>
        {
            var manager = CreateManager();
            var changes = 0;
            manager.StateChanged += (_, _) => changes++;
            try
            {
                manager.RestoreImages([new() { FilePath = _imagePath, Left = 4000 }]);
                Assert.False(manager.IsRestoring);
                Assert.Equal(0, changes);
                manager.Items[0].Window!.SetPosition(200, 100);
                Assert.True(changes > 0);
            }
            finally
            {
                manager.RemoveAll();
            }
        });
    }

    [Fact]
    public void RestoredGroupAndRescue_PreserveRelativePositions()
    {
        WpfTestHelper.Run(() =>
        {
            var manager = CreateManager();
            var groupId = Guid.NewGuid();
            try
            {
                manager.RestoreImages([
                    new() { FilePath = _imagePath, Left = 2500, Top = 100, GroupId = groupId },
                    new() { FilePath = _imagePath, Left = 2700, Top = 100, GroupId = groupId }
                ]);
                var first = manager.Items[0];
                var second = manager.Items[1];
                Assert.Equal(200, second.Window!.Left - first.Window!.Left);
                Assert.All(manager.Items, item => Assert.InRange(item.Window!.Left, 0, 1920 - item.Window.Width));

                first.Window.SetPosition(2500, 100);
                second.Window.SetPosition(2700, 100);
                Assert.Equal(2, manager.BringOffscreenImagesIntoView());
                Assert.Equal(200, second.Window.Left - first.Window.Left);
            }
            finally
            {
                manager.RemoveAll();
            }
        });
    }

    [Fact]
    public void RetryAndRelinkUnavailableImages_RetainTheirSettings()
    {
        WpfTestHelper.Run(() =>
        {
            var manager = CreateManager();
            var missingPath = Path.Combine(_directory, "reattached.png");
            try
            {
                manager.RestoreImages([
                    new() { FilePath = missingPath, Left = 200, Top = 100, Scale = 0.5 },
                    new() { FilePath = Path.Combine(_directory, "renamed.png"), Left = 500, Top = 300, RotationDegrees = 90 }
                ]);
                File.Copy(_imagePath, missingPath);
                manager.RetryImage(manager.Items[0]);
                manager.ChangeImage(manager.Items[1], _imagePath);

                Assert.Equal(2, manager.ImageCount);
                Assert.Equal(0, manager.UnavailableCount);
                Assert.Equal(0.5, manager.Items[0].ScaleX);
                Assert.Equal(200, manager.Items[0].Window!.Left);
                Assert.Equal(90, manager.Items[1].RotationDegrees);
                Assert.Equal(500, manager.Items[1].Window!.Left);
            }
            finally
            {
                manager.RemoveAll();
            }
        });
    }

    [Fact]
    public void FailedRetryOrRelink_DoesNotDiscardRegistration()
    {
        WpfTestHelper.Run(() =>
        {
            var manager = CreateManager();
            var missing = Path.Combine(_directory, "missing.png");
            var badImage = Path.Combine(_directory, "bad.png");
            File.WriteAllText(badImage, "invalid");
            manager.RestoreImages([new() { FilePath = missing, Left = 250, Scale = 0.5 }]);
            var item = Assert.Single(manager.Items);

            Assert.Throws<FileNotFoundException>(() => manager.RetryImage(item));
            Assert.ThrowsAny<Exception>(() => manager.ChangeImage(item, badImage));
            Assert.Same(item, Assert.Single(manager.Items));
            Assert.True(item.IsUnavailable);
            Assert.Equal(missing, item.FilePath);
            Assert.Equal(250, item.Left);
            Assert.Equal(0.5, item.Scale);
        });
    }

    [Fact]
    public void NullEntries_AreIgnoredEvenWhenRestoreIsCalledDirectly()
    {
        var manager = CreateManager();
        manager.RestoreImages([null!, new() { FilePath = Path.Combine(_directory, "missing.png") }]);
        Assert.Single(manager.Items);
        Assert.False(manager.IsRestoring);
    }

    [Fact]
    public void DuplicateAfterSourceRemoved_ReportsFailureAndKeepsExistingImageUsable()
    {
        WpfTestHelper.Run(() =>
        {
            var manager = CreateManager();
            try
            {
                var item = manager.AddImage(_imagePath, new() { Left = 100, Top = 100 });
                File.Delete(_imagePath);
                Assert.False(manager.TryDuplicateImage(item, out var error));
                Assert.IsType<FileNotFoundException>(error);
                Assert.Same(item, Assert.Single(manager.Items));
                manager.ScaleImageOrGroup(item, 2, 2);
                Assert.Equal(2, item.ScaleX);
                Assert.False(item.IsUnavailable);
            }
            finally
            {
                manager.RemoveAll();
            }
        });
    }

    [Theory]
    [InlineData(90, 2, 1)]
    [InlineData(270, 1, 2)]
    public void GroupScaling_RotatedWindowMatchesScreenAxes(int rotation, double x, double y)
    {
        WpfTestHelper.Run(() =>
        {
            var manager = CreateManager();
            try
            {
                var first = manager.AddImage(_imagePath, new() { Left = 100, Top = 100 });
                var second = manager.AddImage(_imagePath, new() { Left = 300, Top = 100, RotationDegrees = rotation });
                manager.GroupImages([first, second]);
                var width = second.Window!.Width;
                var height = second.Window.Height;
                manager.ScaleImageOrGroup(first, x, y);
                Assert.Equal(width * x, second.Window.Width, 8);
                Assert.Equal(height * y, second.Window.Height, 8);
            }
            finally
            {
                manager.RemoveAll();
            }
        });
    }

    [Fact]
    public void GroupChanges_AlsoUpdateUnavailableMemberForLaterRetry()
    {
        WpfTestHelper.Run(() =>
        {
            var manager = CreateManager();
            var group = Guid.NewGuid();
            try
            {
                manager.RestoreImages([
                    new() { FilePath = _imagePath, Left = 100, Top = 100, GroupId = group },
                    new() { FilePath = Path.Combine(_directory, "missing.png"), Left = 200, Top = 200, GroupId = group }
                ]);
                manager.ScaleImageOrGroup(manager.Items[0], 2, 2);
                Assert.Equal(300, manager.Items[1].Left);
                Assert.Equal(300, manager.Items[1].Top);
                Assert.Equal(2, manager.Items[1].ScaleX);
                Assert.Equal(group, manager.Items[1].GroupId);
            }
            finally
            {
                manager.RemoveAll();
            }
        });
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }
}
