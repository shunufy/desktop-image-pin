using System.Collections.ObjectModel;
using System.IO;
using DesktopImagePin.Models;
using DesktopImagePin.Windows;

namespace DesktopImagePin.Services;

public sealed class ImageManager
{
    private static readonly HashSet<string> SupportedExtensions = new(
        [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff"],
        StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<ImageItem> Items { get; } = [];

    public ImageItem AddImage(string filePath, SavedImageState? savedState = null)
    {
        EnsureSupportedImage(filePath);

        var item = new ImageItem(filePath);
        if (savedState is not null)
        {
            item.Scale = savedState.Scale;
            item.ScaleX = savedState.ScaleX ?? savedState.Scale;
            item.ScaleY = savedState.ScaleY ?? savedState.Scale;
            item.Left = savedState.Left;
            item.Top = savedState.Top;
            item.DisplayLayer = savedState.DisplayLayer;
        }

        var window = new ImageWindow(item, this);
        item.Window = window;

        window.Closed += (_, _) => OnWindowClosed(item, window);
        Items.Add(item);
        window.Show();

        return item;
    }

    public IReadOnlyList<ImageItem> AddImages(IEnumerable<string> filePaths)
    {
        var addedItems = new List<ImageItem>();

        foreach (var filePath in filePaths.Where(IsSupportedImageFile).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                addedItems.Add(AddImage(filePath));
            }
            catch
            {
                // Continue adding the remaining valid images.
            }
        }

        return addedItems;
    }

    public void RestoreImages(IEnumerable<SavedImageState> savedStates)
    {
        foreach (var savedState in savedStates.Where(state => IsSupportedImageFile(state.FilePath)))
        {
            try
            {
                AddImage(savedState.FilePath, savedState);
            }
            catch
            {
                // A corrupt or inaccessible image should not block application startup.
            }
        }
    }

    public ImageItem DuplicateImage(ImageItem source)
    {
        var duplicateState = new SavedImageState
        {
            FilePath = source.FilePath,
            Scale = source.Scale,
            ScaleX = source.ScaleX,
            ScaleY = source.ScaleY,
            Left = (source.Left ?? source.Window?.Left ?? 0) + 24,
            Top = (source.Top ?? source.Window?.Top ?? 0) + 24,
            DisplayLayer = source.DisplayLayer
        };

        return AddImage(source.FilePath, duplicateState);
    }

    public void ChangeImage(ImageItem item, string filePath)
    {
        EnsureSupportedImage(filePath);

        item.Window?.SetImage(filePath);
        item.FilePath = filePath;
    }

    public void SetDisplayLayer(ImageItem item, ImageDisplayLayer displayLayer)
    {
        item.DisplayLayer = displayLayer;
        item.Window?.SetDisplayLayer(displayLayer);
    }

    public void RemoveImage(ImageItem item)
    {
        Items.Remove(item);

        var window = item.Window;
        item.Window = null;
        window?.Close();
    }

    public void RemoveAll()
    {
        foreach (var item in Items.ToArray())
        {
            RemoveImage(item);
        }
    }

    private void OnWindowClosed(ImageItem item, ImageWindow window)
    {
        if (ReferenceEquals(item.Window, window))
        {
            item.Window = null;
        }

        Items.Remove(item);
    }

    public static bool IsSupportedImageFile(string filePath)
    {
        return File.Exists(filePath) && SupportedExtensions.Contains(Path.GetExtension(filePath));
    }

    private static void EnsureSupportedImage(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The image file could not be found.", filePath);
        }

        if (!SupportedExtensions.Contains(Path.GetExtension(filePath)))
        {
            throw new NotSupportedException("This image format is not supported.");
        }
    }
}
