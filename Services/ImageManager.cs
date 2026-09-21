using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using DesktopImagePin.Models;
using DesktopImagePin.Windows;
using Rect = System.Windows.Rect;

namespace DesktopImagePin.Services;

public sealed class ImageManager : INotifyPropertyChanged
{
    public const double MinimumScale = 0.05;
    public const double MaximumScale = 10.0;

    private static readonly HashSet<string> SupportedExtensions = new(
        [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff"],
        StringComparer.OrdinalIgnoreCase);

    private readonly Func<IReadOnlyList<Rect>> _getWorkAreas;
    private readonly Action<ImageWindow> _showWindow;

    public ObservableCollection<ImageItem> Items { get; } = [];
    public int ImageCount => Items.Count(item => !item.IsUnavailable);
    public int UnavailableCount => Items.Count(item => item.IsUnavailable);
    public bool IsRestoring { get; private set; }

    public ImageManager()
        : this(DesktopWorkAreaProvider.GetWorkAreas, window => window.Show())
    {
    }

    internal ImageManager(Func<IReadOnlyList<Rect>> getWorkAreas, Action<ImageWindow> showWindow)
    {
        _getWorkAreas = getWorkAreas;
        _showWindow = showWindow;
        Items.CollectionChanged += Items_CollectionChanged;
    }

    public ImageItem AddImage(string filePath, SavedImageState? savedState = null)
    {
        EnsureSupportedImage(filePath);

        var item = CreateItem(filePath, savedState);
        try
        {
            CreateWindow(item);
            Items.Add(item);
            BringImagesIntoView(GetTransformTargets(item));
            ShowWindow(item);
            return item;
        }
        catch
        {
            Items.Remove(item);
            CloseFailedWindow(item);
            throw;
        }
    }

    private static ImageItem CreateItem(string filePath, SavedImageState? savedState)
    {
        var item = new ImageItem(filePath);
        if (savedState is not null)
        {
            item.Scale = savedState.Scale;
            item.ScaleX = savedState.ScaleX ?? savedState.Scale;
            item.ScaleY = savedState.ScaleY ?? savedState.Scale;
            item.Left = savedState.Left;
            item.Top = savedState.Top;
            item.DisplayLayer = savedState.DisplayLayer;
            item.Opacity = Math.Clamp(savedState.Opacity, 0.1, 1.0);
            item.RotationDegrees = savedState.RotationDegrees;
            item.FlipHorizontal = savedState.FlipHorizontal;
            item.FlipVertical = savedState.FlipVertical;
            item.IsClickThrough = savedState.IsClickThrough;
            item.GroupId = savedState.GroupId;
        }

        return item;
    }

    private void CreateWindow(ImageItem item)
    {
        EnsureSupportedImage(item.FilePath);
        var window = new ImageWindow(item, this);
        item.Window = window;
        window.Closed += (_, _) => OnWindowClosed(item, window);
    }

    private void ShowWindow(ImageItem item)
    {
        try
        {
            _showWindow(item.Window!);
        }
        catch
        {
            CloseFailedWindow(item);
            throw;
        }
    }

    private static void CloseFailedWindow(ImageItem item)
    {
        var window = item.Window;
        item.Window = null;
        window?.Close();
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
        IsRestoring = true;
        try
        {
            var restoredItems = new List<ImageItem>();
            foreach (var savedState in savedStates)
            {
                if (!ImageStateStore.IsValidState(savedState))
                {
                    continue;
                }

                var item = CreateItem(savedState.FilePath, savedState);
                Items.Add(item);
                restoredItems.Add(item);
                try
                {
                    CreateWindow(item);
                }
                catch
                {
                    // Keep unavailable images and their saved settings for retry or relinking.
                    CloseFailedWindow(item);
                }
            }

            NormalizeGroups(Items.Select(item => item.GroupId).OfType<Guid>().ToArray());
            BringImagesIntoView(restoredItems);
            foreach (var item in restoredItems.Where(item => !item.IsUnavailable))
            {
                try
                {
                    ShowWindow(item);
                }
                catch
                {
                    // A failed window remains in the Hub as an unavailable item.
                }
            }
        }
        finally
        {
            IsRestoring = false;
        }
    }

    public void RetryImage(ImageItem item)
    {
        if (!Items.Contains(item) || !item.IsUnavailable)
        {
            return;
        }

        try
        {
            CreateWindow(item);
            BringImagesIntoView(GetTransformTargets(item));
            ShowWindow(item);
        }
        catch
        {
            CloseFailedWindow(item);
            throw;
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
            DisplayLayer = source.DisplayLayer,
            Opacity = source.Opacity,
            RotationDegrees = source.RotationDegrees,
            FlipHorizontal = source.FlipHorizontal,
            FlipVertical = source.FlipVertical,
            IsClickThrough = source.IsClickThrough
        };

        return AddImage(source.FilePath, duplicateState);
    }

    public bool TryDuplicateImage(ImageItem source, out Exception? error)
    {
        try
        {
            DuplicateImage(source);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            error = exception;
            return false;
        }
    }

    public bool GroupImages(IEnumerable<ImageItem> items)
    {
        var selectedItems = items
            .Where(Items.Contains)
            .Distinct()
            .ToArray();

        if (selectedItems.Length < 2)
        {
            return false;
        }

        var previousGroupIds = selectedItems
            .Select(item => item.GroupId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();
        var groupId = Guid.NewGuid();

        foreach (var item in selectedItems)
        {
            item.GroupId = groupId;
        }

        NormalizeGroups(previousGroupIds);
        return true;
    }

    public bool UngroupImages(IEnumerable<ImageItem> items)
    {
        var groupIds = items
            .Where(Items.Contains)
            .Select(item => item.GroupId)
            .OfType<Guid>()
            .Distinct()
            .ToHashSet();

        if (groupIds.Count == 0)
        {
            return false;
        }

        foreach (var item in Items.Where(item => item.GroupId is Guid id && groupIds.Contains(id)))
        {
            item.GroupId = null;
        }

        return true;
    }

    public IReadOnlyList<ImageItem> GetTransformTargets(ImageItem source)
    {
        if (source.GroupId is not Guid groupId)
        {
            return [source];
        }

        return Items.Where(item => item.GroupId == groupId).ToArray();
    }

    public void ScaleImageOrGroup(ImageItem source, double factorX, double factorY)
    {
        var targets = GetTransformTargets(source);
        var results = GroupTransformCalculator.Scale(
            targets.Select(item => new GroupTransformInput(
                item.Id,
                item.Window?.Left ?? item.Left ?? 0,
                item.Window?.Top ?? item.Top ?? 0,
                item.ScaleX,
                item.ScaleY,
                item.RotationDegrees)),
            source.Id,
            factorX,
            factorY,
            MinimumScale,
            MaximumScale);
        var targetsById = targets.ToDictionary(item => item.Id);

        foreach (var result in results)
        {
            var item = targetsById[result.Id];
            item.ScaleX = result.ScaleX;
            item.ScaleY = result.ScaleY;
            item.Scale = Math.Min(result.ScaleX, result.ScaleY);
            item.Window?.ApplyAppearance();
        }

        foreach (var result in results)
        {
            SetItemPosition(targetsById[result.Id], result.Left, result.Top);
        }
    }

    public int BringOffscreenImagesIntoView()
    {
        return BringImagesIntoView(Items);
    }

    private int BringImagesIntoView(IEnumerable<ImageItem> items)
    {
        var movedCount = 0;
        var workAreas = _getWorkAreas();

        foreach (var group in items.GroupBy(item => item.GroupId ?? item.Id))
        {
            var bounds = group
                .Where(item => item.Window is { } window && double.IsFinite(window.Left) && double.IsFinite(window.Top))
                .Select(item => new ImageWindowBounds(
                    item.Window!.Left, item.Window.Top, item.Window.Width, item.Window.Height))
                .ToArray();
            var offset = WindowPlacementService.GetGroupOffset(bounds, workAreas);
            if (offset.X == 0 && offset.Y == 0)
            {
                continue;
            }

            foreach (var item in group)
            {
                SetItemPosition(item,
                    (item.Window?.Left ?? item.Left ?? 0) + offset.X,
                    (item.Window?.Top ?? item.Top ?? 0) + offset.Y);
                if (!item.IsUnavailable)
                {
                    movedCount++;
                }
            }
        }

        return movedCount;
    }

    internal static void SetItemPosition(ImageItem item, double left, double top)
    {
        if (item.Window is { } window)
        {
            window.SetPosition(left, top);
        }
        else
        {
            item.Left = left;
            item.Top = top;
        }
    }

    public void ChangeImage(ImageItem item, string filePath)
    {
        EnsureSupportedImage(filePath);

        if (item.Window is { } window)
        {
            window.SetImage(filePath);
            item.FilePath = filePath;
            return;
        }

        var previousPath = item.FilePath;
        try
        {
            item.FilePath = filePath;
            RetryImage(item);
        }
        catch
        {
            item.FilePath = previousPath;
            throw;
        }
    }

    public void SetDisplayLayer(ImageItem item, ImageDisplayLayer displayLayer)
    {
        item.DisplayLayer = displayLayer;
        item.Window?.SetDisplayLayer(displayLayer);
    }

    public void SetOpacity(ImageItem item, double opacity)
    {
        item.Opacity = Math.Clamp(opacity, 0.1, 1.0);
        item.Window?.ApplyAppearance();
    }

    public void RotateImage(ImageItem item, int degrees)
    {
        item.RotationDegrees += degrees;
        item.Window?.ApplyAppearance();
    }

    public void ToggleHorizontalFlip(ImageItem item)
    {
        item.FlipHorizontal = !item.FlipHorizontal;
        item.Window?.ApplyAppearance();
    }

    public void ToggleVerticalFlip(ImageItem item)
    {
        item.FlipVertical = !item.FlipVertical;
        item.Window?.ApplyAppearance();
    }

    public void SetClickThrough(ImageItem item, bool isClickThrough)
    {
        item.IsClickThrough = isClickThrough;
        item.Window?.SetClickThrough(isClickThrough);
    }

    public void RemoveImage(ImageItem item)
    {
        var previousGroupId = item.GroupId;
        Items.Remove(item);

        var window = item.Window;
        item.Window = null;
        window?.Close();

        if (previousGroupId is Guid groupId)
        {
            NormalizeGroups([groupId]);
        }
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
        if (!ReferenceEquals(item.Window, window))
        {
            return;
        }

        item.Window = null;
        var previousGroupId = item.GroupId;
        Items.Remove(item);
        if (previousGroupId is Guid groupId)
        {
            NormalizeGroups([groupId]);
        }
    }

    private void NormalizeGroups(IEnumerable<Guid> groupIds)
    {
        foreach (var groupId in groupIds.Distinct())
        {
            var members = Items.Where(item => item.GroupId == groupId).ToArray();
            if (members.Length == 1)
            {
                members[0].GroupId = null;
            }
        }
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

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? StateChanged;

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ImageItem item in e.OldItems)
            {
                item.PropertyChanged -= ImageItem_PropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (ImageItem item in e.NewItems)
            {
                item.PropertyChanged += ImageItem_PropertyChanged;
            }
        }

        OnPropertyChanged(nameof(ImageCount));
        OnPropertyChanged(nameof(UnavailableCount));
        NotifyStateChanged();
    }

    private void ImageItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImageItem.Window))
        {
            OnPropertyChanged(nameof(ImageCount));
            OnPropertyChanged(nameof(UnavailableCount));
        }

        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        if (!IsRestoring)
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
