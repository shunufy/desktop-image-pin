using System.Text.Json;
using DesktopImagePin.Models;
using DesktopImagePin.Services;

namespace DesktopImagePin.Tests;

public sealed class UrlImportLibraryTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"DesktopImagePin.UrlLibraryTests-{Guid.NewGuid():N}");

    [Fact]
    public void Constructor_LoadsSavedEntriesWithoutDownloading()
    {
        Directory.CreateDirectory(_directory);
        var statePath = Path.Combine(_directory, "url-imports.json");
        var item = new SavedUrlImport
        {
            Name = "Reference",
            Url = "https://example.com/reference.png",
            LocalFilePath = Path.Combine(_directory, "reference.png")
        };
        File.WriteAllText(statePath, JsonSerializer.Serialize(new[] { item }));

        var library = new UrlImportLibrary(
            new ImageImportService(Path.Combine(_directory, "cache")),
            statePath);

        var restored = Assert.Single(library.Items);
        Assert.Equal(item.Name, restored.Name);
        Assert.Equal(item.Url, restored.Url);
        Assert.Equal(item.LocalFilePath, restored.LocalFilePath);
    }

    [Fact]
    public void Delete_RemovesEntryButCanPreserveCachedFile()
    {
        Directory.CreateDirectory(_directory);
        var cacheDirectory = Path.Combine(_directory, "cache");
        Directory.CreateDirectory(cacheDirectory);
        var cachedFile = Path.Combine(cacheDirectory, "reference.png");
        File.WriteAllBytes(cachedFile, [1, 2, 3]);
        var statePath = Path.Combine(_directory, "url-imports.json");
        File.WriteAllText(
            statePath,
            JsonSerializer.Serialize(new[]
            {
                new SavedUrlImport
                {
                    Name = "Reference",
                    Url = "https://example.com/reference.png",
                    LocalFilePath = cachedFile
                }
            }));
        var library = new UrlImportLibrary(
            new ImageImportService(cacheDirectory),
            statePath);

        library.Delete(library.Items[0], deleteCachedFile: false);

        Assert.Empty(library.Items);
        Assert.True(File.Exists(cachedFile));
        Assert.Equal("[]", File.ReadAllText(statePath).Trim());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
