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
    private readonly string _backupFilePath;

    public ImageStateStore(string? filePath = null)
    {
        if (filePath is null)
        {
            var appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DesktopImagePin");

            filePath = Path.Combine(appDataDirectory, "images.json");
        }

        _filePath = Path.GetFullPath(filePath);
        _backupFilePath = Path.Combine(
            Path.GetDirectoryName(_filePath)!,
            $"{Path.GetFileNameWithoutExtension(_filePath)}.backup{Path.GetExtension(_filePath)}");
    }

    public IReadOnlyList<SavedImageState> Load()
    {
        var primaryStates = TryLoad(_filePath);
        if (primaryStates is not null)
        {
            return primaryStates;
        }

        return TryLoad(_backupFilePath) ?? [];
    }

    private static List<SavedImageState>? TryLoad(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var states = JsonSerializer.Deserialize<List<SavedImageState>>(json, JsonOptions);
            return states is not null && states.All(IsValidState) ? states : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
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
            IsClickThrough = item.IsClickThrough,
            GroupId = item.GroupId
        }).ToArray();

        if (!states.All(IsValidState))
        {
            throw new InvalidDataException("The image layout contains invalid values and cannot be saved.");
        }

        var directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);

        var temporaryPath = _filePath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(states, JsonOptions));

            if (File.Exists(_filePath))
            {
                // Never replace a usable backup with a corrupt primary file.
                var backupPath = TryLoad(_filePath) is not null ? _backupFilePath : null;
                File.Replace(temporaryPath, _filePath, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, _filePath);
            }
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                // Preserve the original save error if cleanup is not possible.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    internal static bool IsValidState(SavedImageState? state)
    {
        return state is not null
            && !string.IsNullOrWhiteSpace(state.FilePath)
            && double.IsFinite(state.Left)
            && double.IsFinite(state.Top)
            && IsValidScale(state.Scale)
            && IsValidScale(state.ScaleX ?? state.Scale)
            && IsValidScale(state.ScaleY ?? state.Scale)
            && double.IsFinite(state.Opacity)
            && state.Opacity is >= 0.1 and <= 1.0
            && state.RotationDegrees % 90 == 0
            && Enum.IsDefined(state.DisplayLayer);
    }

    private static bool IsValidScale(double scale)
    {
        return double.IsFinite(scale)
            && scale >= ImageManager.MinimumScale
            && scale <= ImageManager.MaximumScale;
    }
}
