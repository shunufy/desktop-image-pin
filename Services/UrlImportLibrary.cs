using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using DesktopImagePin.Models;

namespace DesktopImagePin.Services;

public sealed class UrlImportLibrary
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ImageImportService _imageImportService;
    private readonly string _stateFilePath;

    public UrlImportLibrary(ImageImportService imageImportService, string? stateFilePath = null)
    {
        _imageImportService = imageImportService;

        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopImagePin");

        _stateFilePath = stateFilePath ?? Path.Combine(appDataDirectory, "url-imports.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);

        foreach (var item in Load())
        {
            Items.Add(item);
        }
    }

    public ObservableCollection<SavedUrlImport> Items { get; } = [];

    public async Task<SavedUrlImport> SaveAsync(string name, string url)
    {
        name = name.Trim();
        url = url.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Enter a name.");
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Enter an image URL.");
        }

        var localFilePath = await _imageImportService.ImportFromUrlAsync(url);
        var existing = Items.FirstOrDefault(
            item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new SavedUrlImport
            {
                Name = name,
                Url = url,
                LocalFilePath = localFilePath,
                SavedAt = DateTimeOffset.Now
            };
            Items.Add(existing);
        }
        else
        {
            existing.Url = url;
            existing.LocalFilePath = localFilePath;
            existing.SavedAt = DateTimeOffset.Now;
            RefreshItem(existing);
        }

        Save();
        return existing;
    }

    public async Task RefreshAsync(SavedUrlImport item)
    {
        item.LocalFilePath = await _imageImportService.ImportFromUrlAsync(item.Url);
        item.SavedAt = DateTimeOffset.Now;
        RefreshItem(item);
        Save();
    }

    public void Delete(SavedUrlImport item, bool deleteCachedFile)
    {
        if (!Items.Remove(item))
        {
            return;
        }

        if (deleteCachedFile)
        {
            TryDeleteManagedFile(item.LocalFilePath);
        }

        Save();
    }

    private List<SavedUrlImport> Load()
    {
        if (!File.Exists(_stateFilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_stateFilePath);
            return JsonSerializer.Deserialize<List<SavedUrlImport>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private void Save()
    {
        var temporaryPath = _stateFilePath + ".tmp";
        var json = JsonSerializer.Serialize(Items, JsonOptions);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, _stateFilePath, true);
    }

    private void RefreshItem(SavedUrlImport item)
    {
        var index = Items.IndexOf(item);
        if (index >= 0)
        {
            Items[index] = item;
        }
    }

    private void TryDeleteManagedFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !_imageImportService.IsManagedFile(filePath))
        {
            return;
        }

        try
        {
            File.Delete(filePath);
        }
        catch (IOException)
        {
            // A displayed image may still reference the cache file.
        }
        catch (UnauthorizedAccessException)
        {
            // Keep the library entry removal independent from cache cleanup.
        }
    }
}
