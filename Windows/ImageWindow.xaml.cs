using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopImagePin.Models;
using DesktopImagePin.Services;
using Microsoft.Win32;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using IDataObject = System.Windows.IDataObject;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;

namespace DesktopImagePin.Windows;

public partial class ImageWindow : Window
{
    private static readonly IntPtr HwndBottom = new(1);
    private static readonly IntPtr HwndNotTopmost = new(-2);
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;

    private const double ScaleStep = 1.1;
    private const double InitialWorkAreaRatio = 0.9;

    private readonly ImageItem _item;
    private readonly ImageManager _imageManager;
    private double _imageWidth;
    private double _imageHeight;
    private Point _dragStartPointer;
    private Dictionary<ImageItem, (double Left, double Top)>? _dragStartPositions;

    public ImageWindow(ImageItem item, ImageManager imageManager)
    {
        InitializeComponent();

        _item = item;
        _imageManager = imageManager;
        var hasSavedPosition = item.Left.HasValue && item.Top.HasValue;
        if (hasSavedPosition)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        SetImage(item.FilePath, fitToWorkArea: !hasSavedPosition);
        if (hasSavedPosition)
        {
            SetPosition(item.Left!.Value, item.Top!.Value);
        }
        SourceInitialized += (_, _) =>
        {
            SetDisplayLayer(_item.DisplayLayer);
            SetClickThrough(_item.IsClickThrough);
        };
        LocationChanged += (_, _) =>
        {
            _item.Left = Left;
            _item.Top = Top;
        };
        Loaded += (_, _) =>
        {
            _item.Left = Left;
            _item.Top = Top;
        };
        DisplayedImage.ContextMenu.Closed += (_, _) => RestoreBottommostPosition();
    }

    public void SetImage(string filePath, bool fitToWorkArea = false)
    {
        var bitmap = LoadBitmap(filePath);

        DisplayedImage.Source = bitmap;
        _imageWidth = bitmap.Width;
        _imageHeight = bitmap.Height;

        if (fitToWorkArea)
        {
            var initialScale = CalculateInitialScale();
            SetScale(initialScale, initialScale);
            return;
        }

        SetScale(_item.ScaleX, _item.ScaleY);
    }

    public void ApplyAppearance()
    {
        Opacity = _item.Opacity;

        var transform = new TransformGroup();
        transform.Children.Add(new ScaleTransform(
            _item.FlipHorizontal ? -1 : 1,
            _item.FlipVertical ? -1 : 1));
        transform.Children.Add(new RotateTransform(_item.RotationDegrees));
        DisplayedImage.LayoutTransform = transform;

        var imageDisplayWidth = Math.Max(1, _imageWidth * _item.ScaleX);
        var imageDisplayHeight = Math.Max(1, _imageHeight * _item.ScaleY);
        DisplayedImage.Width = imageDisplayWidth;
        DisplayedImage.Height = imageDisplayHeight;

        var swapsDimensions = _item.RotationDegrees is 90 or 270;
        Width = swapsDimensions ? imageDisplayHeight : imageDisplayWidth;
        Height = swapsDimensions ? imageDisplayWidth : imageDisplayHeight;
    }

    public void SetClickThrough(bool isClickThrough)
    {
        _item.IsClickThrough = isClickThrough;

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var extendedStyle = GetWindowLong(handle, GwlExStyle);
        var updatedStyle = isClickThrough
            ? extendedStyle | WsExTransparent
            : extendedStyle & ~WsExTransparent;

        if (updatedStyle != extendedStyle)
        {
            _ = SetWindowLong(handle, GwlExStyle, updatedStyle);
            SetWindowPos(
                handle,
                IntPtr.Zero,
                0,
                0,
                0,
                0,
                SwpNoMove | SwpNoSize | SwpNoActivate | SwpFrameChanged);
        }
    }

    public void SetDisplayLayer(ImageDisplayLayer displayLayer)
    {
        _item.DisplayLayer = displayLayer;
        Topmost = displayLayer == ImageDisplayLayer.Topmost;

        TopmostMenuItem.IsChecked = displayLayer == ImageDisplayLayer.Topmost;
        NormalLayerMenuItem.IsChecked = displayLayer == ImageDisplayLayer.Normal;
        BottommostMenuItem.IsChecked = displayLayer == ImageDisplayLayer.Bottommost;

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var insertAfter = displayLayer == ImageDisplayLayer.Bottommost
            ? HwndBottom
            : HwndNotTopmost;

        if (displayLayer != ImageDisplayLayer.Topmost)
        {
            SetWindowPos(
                handle,
                insertAfter,
                0,
                0,
                0,
                0,
                SwpNoMove | SwpNoSize | SwpNoActivate);
        }
    }

    private static BitmapImage LoadBitmap(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private double CalculateInitialScale()
    {
        var workArea = SystemParameters.WorkArea;
        var maximumWidth = workArea.Width * InitialWorkAreaRatio;
        var maximumHeight = workArea.Height * InitialWorkAreaRatio;
        var widthScale = maximumWidth / _imageWidth;
        var heightScale = maximumHeight / _imageHeight;

        return Math.Min(1.0, Math.Min(widthScale, heightScale));
    }

    public void SetScale(double scaleX, double scaleY)
    {
        var clampedScaleX = Math.Clamp(
            scaleX,
            ImageManager.MinimumScale,
            ImageManager.MaximumScale);
        var clampedScaleY = Math.Clamp(
            scaleY,
            ImageManager.MinimumScale,
            ImageManager.MaximumScale);
        _item.ScaleX = clampedScaleX;
        _item.ScaleY = clampedScaleY;
        _item.Scale = Math.Min(clampedScaleX, clampedScaleY);
        ApplyAppearance();
    }

    public void SetPosition(double left, double top)
    {
        Left = left;
        Top = top;
        _item.Left = left;
        _item.Top = top;
    }

    private void DisplayedImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        _dragStartPointer = GetMouseScreenPosition(e);
        _dragStartPositions = _imageManager
            .GetTransformTargets(_item)
            .ToDictionary(
                item => item,
                item => (
                    Left: item.Window?.Left ?? item.Left ?? 0,
                    Top: item.Window?.Top ?? item.Top ?? 0));
        DisplayedImage.CaptureMouse();
        e.Handled = true;
    }

    private void DisplayedImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStartPositions is null)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndDrag();
            return;
        }

        var pointer = GetMouseScreenPosition(e);
        var deltaX = pointer.X - _dragStartPointer.X;
        var deltaY = pointer.Y - _dragStartPointer.Y;

        foreach (var (item, position) in _dragStartPositions)
        {
            ImageManager.SetItemPosition(item,
                position.Left + deltaX,
                position.Top + deltaY);
        }
    }

    private void DisplayedImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndDrag();
        e.Handled = true;
    }

    private void DisplayedImage_LostMouseCapture(object sender, MouseEventArgs e)
    {
        _dragStartPositions = null;
        RestoreBottommostPosition();
    }

    private void EndDrag()
    {
        _dragStartPositions = null;
        if (DisplayedImage.IsMouseCaptured)
        {
            DisplayedImage.ReleaseMouseCapture();
        }

        RestoreBottommostPosition();
    }

    private Point GetMouseScreenPosition(MouseEventArgs e)
    {
        var devicePoint = PointToScreen(e.GetPosition(this));
        var presentationSource = PresentationSource.FromVisual(this);
        return presentationSource?.CompositionTarget?.TransformFromDevice.Transform(devicePoint)
            ?? devicePoint;
    }

    private void DisplayedImage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var factor = e.Delta > 0 ? ScaleStep : 1.0 / ScaleStep;
        var modifiers = Keyboard.Modifiers;
        var controlOnly = modifiers.HasFlag(ModifierKeys.Control)
            && !modifiers.HasFlag(ModifierKeys.Alt);
        var altOnly = modifiers.HasFlag(ModifierKeys.Alt)
            && !modifiers.HasFlag(ModifierKeys.Control);

        if (controlOnly)
        {
            _imageManager.ScaleImageOrGroup(_item, factor, 1.0);
        }
        else if (altOnly)
        {
            _imageManager.ScaleImageOrGroup(_item, 1.0, factor);
        }
        else
        {
            _imageManager.ScaleImageOrGroup(_item, factor, factor);
        }

        e.Handled = true;
    }

    private void ChangeImageMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select an image to display",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _imageManager.ChangeImage(_item, dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Could not load the image.\n\n{ex.Message}",
                "Image Loading Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void DuplicateMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (!_imageManager.TryDuplicateImage(_item, out var error))
        {
            MessageBox.Show(this,
                $"Could not duplicate the image.\n\n{error!.Message}",
                "Image Duplication Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ZoomInMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _imageManager.ScaleImageOrGroup(_item, ScaleStep, ScaleStep);
    }

    private void ZoomOutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _imageManager.ScaleImageOrGroup(_item, 1.0 / ScaleStep, 1.0 / ScaleStep);
    }

    private void UngroupMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _imageManager.UngroupImages([_item]);
    }

    private void TopmostMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _imageManager.SetDisplayLayer(_item, ImageDisplayLayer.Topmost);
    }

    private void NormalLayerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _imageManager.SetDisplayLayer(_item, ImageDisplayLayer.Normal);
    }

    private void BottommostMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _imageManager.SetDisplayLayer(_item, ImageDisplayLayer.Bottommost);
    }

    private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _imageManager.RemoveImage(_item);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasSupportedDroppedFiles(e.Data)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] filePaths)
        {
            _imageManager.AddImages(filePaths);
        }
    }

    private static bool HasSupportedDroppedFiles(IDataObject data)
    {
        return data.GetData(DataFormats.FileDrop) is string[] filePaths
            && filePaths.Any(ImageManager.IsSupportedImageFile);
    }

    private void RestoreBottommostPosition()
    {
        if (_item.DisplayLayer != ImageDisplayLayer.Bottommost)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            () => SetDisplayLayer(ImageDisplayLayer.Bottommost),
            DispatcherPriority.ApplicationIdle);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr windowHandle, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr windowHandle, int index, int newLong);
}
