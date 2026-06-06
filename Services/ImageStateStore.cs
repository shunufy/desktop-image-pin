using System.IO;
using System.Text.Json;
using DesktopImagePin.Models;

namespace DesktopImagePin.Services;

public sealed class ImageStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public ImageStateStore()
    {
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopImagePin");

        _filePath = Path.Combine(appDataDirectory, "images.json");
    }

    public IReadOnlyList<SavedImageState> Load()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<SavedImageState>>(json, JsonOptions) ?? [];
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

    public void Save(IEnumerable<ImageItem> items)
    {
        var states = items.Select(item => new SavedImageState
        {
            FilePath = item.FilePath,
            Scale = item.Scale,
            ScaleX = item.ScaleX,
            ScaleY = item.ScaleY,
            Left = item.Left ?? item.Window?.Left ?? 0,
            Top = item.Top ?? item.Window?.Top ?? 0,
            DisplayLayer = item.DisplayLayer,
            Opacity = item.Opacity,
            RotationDegrees = item.RotationDegrees,
            FlipHorizontal = item.FlipHorizontal,
            FlipVertical = item.FlipVertical,
            IsClickThrough = item.IsClickThrough
        }).ToArray();

        var directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);

        var temporaryPath = _filePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(states, JsonOptions));
        File.Move(temporaryPath, _filePath, true);
    }
}
