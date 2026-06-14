using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using Clipboard = System.Windows.Clipboard;

namespace DesktopImagePin.Services;

public sealed class ImageImportService
{
    public const long MaximumDownloadBytes = 25 * 1024 * 1024;

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly string _importDirectory;

    public ImageImportService(string? importDirectory = null)
    {
        _importDirectory = importDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopImagePin",
            "ImportedImages");
    }

    public bool IsManagedFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            var importDirectory = Path.GetFullPath(_importDirectory)
                .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var candidatePath = Path.GetFullPath(filePath);

            return candidatePath.StartsWith(importDirectory, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public string? ImportClipboardImage()
    {
        if (Clipboard.ContainsFileDropList())
        {
            var droppedFilePath = Clipboard.GetFileDropList()
                .Cast<string>()
                .FirstOrDefault(ImageManager.IsSupportedImageFile);
            if (droppedFilePath is not null)
            {
                return droppedFilePath;
            }
        }

        if (!Clipboard.ContainsImage())
        {
            return null;
        }

        var bitmap = Clipboard.GetImage();
        if (bitmap is null)
        {
            return null;
        }

        Directory.CreateDirectory(_importDirectory);
        var importedFilePath = CreateImportedPath(".png");

        using (var stream = File.Create(importedFilePath))
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
        }

        return importedFilePath;
    }

    public async Task<string> ImportFromUrlAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Enter an HTTP or HTTPS image URL.");
        }

        using var response = await HttpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength > MaximumDownloadBytes)
        {
            throw new InvalidOperationException("The image exceeds the 25 MB download limit.");
        }

        var extension = GetExtension(uri, response.Content.Headers.ContentType?.MediaType);
        Directory.CreateDirectory(_importDirectory);
        var filePath = CreateImportedPath(extension);

        try
        {
            await DownloadToFileAsync(response, filePath);
            ValidateImage(filePath);
            return filePath;
        }
        catch
        {
            TryDeleteFile(filePath);
            throw;
        }
    }

    private static async Task DownloadToFileAsync(HttpResponseMessage response, string filePath)
    {
        await using var source = await response.Content.ReadAsStreamAsync();
        await using var destination = File.Create(filePath);
        var buffer = new byte[81920];
        long totalBytes = 0;

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
            if (totalBytes > MaximumDownloadBytes)
            {
                throw new InvalidOperationException("The image exceeds the 25 MB download limit.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
        }
    }

    private static void ValidateImage(string filePath)
    {
        if (!ImageManager.IsSupportedImageFile(filePath))
        {
            throw new InvalidOperationException("This image format is not supported.");
        }

        try
        {
            using var validationStream = File.OpenRead(filePath);
            var decoder = BitmapDecoder.Create(
                validationStream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            if (decoder.Frames.Count == 0)
            {
                throw new InvalidOperationException();
            }
        }
        catch (InvalidOperationException exception)
            when (exception.Message is "This image format is not supported.")
        {
            throw;
        }
        catch
        {
            throw new InvalidOperationException("The downloaded data is not a valid image.");
        }
    }

    private string CreateImportedPath(string extension)
    {
        return Path.Combine(
            _importDirectory,
            $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}{extension}");
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch
        {
            // Preserve the original download or validation error.
        }
    }

    internal static string GetExtension(Uri uri, string? mediaType)
    {
        var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        if (extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tif" or ".tiff")
        {
            return extension;
        }

        return mediaType?.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/bmp" => ".bmp",
            "image/gif" => ".gif",
            "image/tiff" => ".tiff",
            _ => ".png"
        };
    }
}
