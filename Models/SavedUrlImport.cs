using System.IO;

namespace DesktopImagePin.Models;

public sealed class SavedUrlImport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string LocalFilePath { get; set; } = string.Empty;

    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.Now;

    public string CacheStatus => File.Exists(LocalFilePath) ? "Cached" : "Local file missing";
}
