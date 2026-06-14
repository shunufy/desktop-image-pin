using DesktopImagePin.Models;

namespace DesktopImagePin.Tests;

public sealed class ImageItemTests
{
    [Theory]
    [InlineData(450, 90)]
    [InlineData(-90, 270)]
    [InlineData(720, 0)]
    public void RotationDegrees_NormalizesValue(int input, int expected)
    {
        var item = new ImageItem("sample.png");

        item.RotationDegrees = input;

        Assert.Equal(expected, item.RotationDegrees);
    }

    [Fact]
    public void TransformText_DescribesRotationAndFlips()
    {
        var item = new ImageItem("sample.png")
        {
            RotationDegrees = 90,
            FlipHorizontal = true,
            FlipVertical = true
        };

        Assert.Equal("90° / Flip H / Flip V", item.TransformText);
    }

    [Fact]
    public void DisplayLayerText_ReflectsLayer()
    {
        var item = new ImageItem("sample.png")
        {
            DisplayLayer = ImageDisplayLayer.Topmost
        };

        Assert.Equal("Always on Top", item.DisplayLayerText);
    }
}
