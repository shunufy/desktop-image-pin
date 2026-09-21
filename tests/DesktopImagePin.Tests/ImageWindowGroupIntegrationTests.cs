using System.Runtime.ExceptionServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DesktopImagePin.Models;
using DesktopImagePin.Services;
using DesktopImagePin.Windows;

namespace DesktopImagePin.Tests;

public sealed class ImageWindowGroupIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"DesktopImagePin.WindowTests-{Guid.NewGuid():N}");

    [Fact]
    public void GroupScale_UpdatesRealImageWindowsAndSpacing()
    {
        Directory.CreateDirectory(_directory);
        var firstPath = Path.Combine(_directory, "first.png");
        var secondPath = Path.Combine(_directory, "second.png");
        SaveTestImage(firstPath);
        SaveTestImage(secondPath);

        RunOnStaThread(() =>
        {
            var manager = new ImageManager();
            var first = new ImageItem(firstPath);
            var second = new ImageItem(secondPath);
            manager.Items.Add(first);
            manager.Items.Add(second);

            var firstWindow = new ImageWindow(first, manager);
            var secondWindow = new ImageWindow(second, manager);
            first.Window = firstWindow;
            second.Window = secondWindow;
            firstWindow.SetPosition(100, 100);
            secondWindow.SetPosition(220, 160);

            Assert.True(manager.GroupImages([first, second]));
            manager.ScaleImageOrGroup(first, factorX: 1.5, factorY: 2);

            Assert.Equal(100, firstWindow.Left, precision: 8);
            Assert.Equal(100, firstWindow.Top, precision: 8);
            Assert.Equal(280, secondWindow.Left, precision: 8);
            Assert.Equal(220, secondWindow.Top, precision: 8);
            Assert.Equal(1.5, first.ScaleX, precision: 8);
            Assert.Equal(2, first.ScaleY, precision: 8);
            Assert.Equal(1.5, second.ScaleX, precision: 8);
            Assert.Equal(2, second.ScaleY, precision: 8);

            firstWindow.Close();
            secondWindow.Close();
        });
    }

    private static void SaveTestImage(string filePath)
    {
        const int width = 64;
        const int height = 48;
        const int stride = width * 4;
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
        var encoder = new PngBitmapEncoder();
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
            Directory.Delete(_directory, recursive: true);
        }
    }
}
