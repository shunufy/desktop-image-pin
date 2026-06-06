namespace DesktopImagePin.Models;

public sealed class SavedImageState
{
    public string FilePath { get; set; } = string.Empty;
    public double Scale { get; set; } = 1.0;
    public double? ScaleX { get; set; }
    public double? ScaleY { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public ImageDisplayLayer DisplayLayer { get; set; } = ImageDisplayLayer.Normal;
}
