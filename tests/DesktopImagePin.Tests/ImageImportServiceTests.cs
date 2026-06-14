using DesktopImagePin.Services;

namespace DesktopImagePin.Tests;

public sealed class ImageImportServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"DesktopImagePin.ImportTests-{Guid.NewGuid():N}");

    [Theory]
    [InlineData("https://example.com/image.jpg", null, ".jpg")]
    [InlineData("https://example.com/image", "image/png", ".png")]
    [InlineData("https://example.com/image", "image/jpeg", ".jpg")]
    [InlineData("https://example.com/image.unknown", null, ".png")]
    public void GetExtension_UsesUrlOrContentType(
        string url,
        string? mediaType,
        string expected)
    {
        var result = ImageImportService.GetExtension(new Uri(url), mediaType);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsManagedFile_OnlyAcceptsFilesUnderImportDirectory()
    {
        Directory.CreateDirectory(_directory);
        var service = new ImageImportService(_directory);

        Assert.True(service.IsManagedFile(Path.Combine(_directory, "image.png")));
        Assert.False(service.IsManagedFile(Path.Combine(Path.GetTempPath(), "outside.png")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
