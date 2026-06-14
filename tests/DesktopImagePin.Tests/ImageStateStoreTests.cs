using DesktopImagePin.Models;
using DesktopImagePin.Services;

namespace DesktopImagePin.Tests;

public sealed class ImageStateStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"DesktopImagePin.Tests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndLoad_RoundTripsImageSettings()
    {
        Directory.CreateDirectory(_directory);
        var statePath = Path.Combine(_directory, "images.json");
        var store = new ImageStateStore(statePath);
        var item = new ImageItem(@"C:\images\sample.png")
        {
            Scale = 0.75,
            ScaleX = 1.2,
            ScaleY = 0.8,
            Left = 120,
            Top = 240,
            DisplayLayer = ImageDisplayLayer.Bottommost,
            Opacity = 0.65,
            RotationDegrees = 270,
            FlipHorizontal = true,
            IsClickThrough = true
        };

        store.Save([item]);
        var restored = Assert.Single(store.Load());

        Assert.Equal(item.FilePath, restored.FilePath);
        Assert.Equal(1.2, restored.ScaleX);
        Assert.Equal(0.8, restored.ScaleY);
        Assert.Equal(120, restored.Left);
        Assert.Equal(240, restored.Top);
        Assert.Equal(ImageDisplayLayer.Bottommost, restored.DisplayLayer);
        Assert.Equal(0.65, restored.Opacity);
        Assert.Equal(270, restored.RotationDegrees);
        Assert.True(restored.FlipHorizontal);
        Assert.True(restored.IsClickThrough);
    }

    [Fact]
    public void Load_ReturnsEmptyListForMalformedJson()
    {
        Directory.CreateDirectory(_directory);
        var statePath = Path.Combine(_directory, "images.json");
        File.WriteAllText(statePath, "{not-json");

        var result = new ImageStateStore(statePath).Load();

        Assert.Empty(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
