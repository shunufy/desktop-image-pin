using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DesktopImagePin.Models;
using DesktopImagePin.Services;
using DesktopImagePin.Windows;

namespace DesktopImagePin.Tests;

public sealed class ImageWindowIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"DesktopImagePin.WindowTests-{Guid.NewGuid():N}");

    [Theory]
    [InlineData("png")]
    [InlineData("jpg")]
    [InlineData("bmp")]
    [InlineData("gif")]
    [InlineData("tiff")]
    public void ImageWindow_LoadsSupportedFormat(string extension)
    {
        Directory.CreateDirectory(_directory);
        var filePath = Path.Combine(_directory, $"sample.{extension}");
        SaveTestImage(filePath, extension, 64, 48);

        RunOnStaThread(() =>
        {
            var item = new ImageItem(filePath);
            var window = new ImageWindow(item, new ImageManager());

            Assert.InRange(window.Width, 63.9, 64.1);
            Assert.InRange(window.Height, 47.9, 48.1);
            window.Close();
        });
    }

    [Fact]
    public void ImageWindow_FitsOversizedImageToWorkArea()
    {
        Directory.CreateDirectory(_directory);
        var filePath = Path.Combine(_directory, "large.png");
        SaveTestImage(filePath, "png", 2000, 1500);

        RunOnStaThread(() =>
        {
            var item = new ImageItem(filePath);
            var window = new ImageWindow(item, new ImageManager());

            Assert.True(window.Width <= SystemParameters.WorkArea.Width * 0.9 + 1);
            Assert.True(window.Height <= SystemParameters.WorkArea.Height * 0.9 + 1);
            Assert.True(item.ScaleX < 1);
            Assert.True(item.ScaleY < 1);
            window.Close();
        });
    }

    private static void SaveTestImage(
        string filePath,
        string extension,
        int width,
        int height)
    {
        var stride = width * 4;
        var pixels = new byte[stride * height];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = 180;
            pixels[index + 1] = 100;
            pixels[index + 2] = 40;
            pixels[index + 3] = 255;
        }

        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);

        BitmapEncoder encoder = extension switch
        {
            "png" => new PngBitmapEncoder(),
            "jpg" => new JpegBitmapEncoder(),
            "bmp" => new BmpBitmapEncoder(),
            "gif" => new GifBitmapEncoder(),
            "tiff" => new TiffBitmapEncoder(),
            _ => throw new ArgumentOutOfRangeException(nameof(extension))
        };

        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(filePath);
        encoder.Save(stream);
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception caught)
            {
                exception = caught;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
