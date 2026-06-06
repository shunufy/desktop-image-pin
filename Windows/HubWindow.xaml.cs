using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopImagePin.Models;
using DesktopImagePin.Services;
using Microsoft.Win32;
using DragEventArgs = System.Windows.DragEventArgs;
using IDataObject = System.Windows.IDataObject;
using Button = System.Windows.Controls.Button;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DesktopImagePin.Windows;

public partial class HubWindow : Window
{
    private readonly ImageManager _imageManager;
    private GlobalHotkeyService? _hotkeyService;

    public HubWindow(ImageManager imageManager)
    {
        InitializeComponent();

        _imageManager = imageManager;
        DataContext = imageManager;

        SourceInitialized += HubWindow_SourceInitialized;
        Closing += HubWindow_Closing;
        Closed += HubWindow_Closed;
    }

    private void HubWindow_SourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            _hotkeyService = new GlobalHotkeyService(
                this,
                HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.NoRepeat,
                Key.H,
                App.Current.ToggleHubWindow);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Could not register Ctrl + Shift + H.\nAnother application may already be using it.\n\n{ex.Message}",
                "Hotkey Registration Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void AddImageButton_Click(object sender, RoutedEventArgs e)
    {
        ShowAddImageDialog();
    }

    public void ShowAddImageDialog()
    {
        var wasEmpty = _imageManager.Items.Count == 0;
        var filePaths = SelectImageFiles();
        if (filePaths.Count == 0)
        {
            return;
        }

        try
        {
            _imageManager.AddImages(filePaths);

            if (wasEmpty && _imageManager.Items.Count > 0)
            {
                Hide();
            }
        }
        catch (Exception ex)
        {
            ShowImageError(ex);
        }
    }

    private void DuplicateSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (ImagesListBox.SelectedItem is ImageItem item)
        {
            _imageManager.DuplicateImage(item);
        }
    }

    private void ChangeImageButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not ImageItem item)
        {
            return;
        }

        ChangeImage(item);
    }

    private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ImageItem item)
        {
            _imageManager.RemoveImage(item);
        }
    }

    private void SetTopmostButton_Click(object sender, RoutedEventArgs e)
    {
        SetDisplayLayer(sender, ImageDisplayLayer.Topmost);
    }

    private void SetNormalLayerButton_Click(object sender, RoutedEventArgs e)
    {
        SetDisplayLayer(sender, ImageDisplayLayer.Normal);
    }

    private void SetBottommostButton_Click(object sender, RoutedEventArgs e)
    {
        SetDisplayLayer(sender, ImageDisplayLayer.Bottommost);
    }

    private void SetDisplayLayer(object sender, ImageDisplayLayer displayLayer)
    {
        if ((sender as Button)?.Tag is ImageItem item)
        {
            _imageManager.SetDisplayLayer(item, displayLayer);
        }
    }

    private void RemoveAllButton_Click(object sender, RoutedEventArgs e)
    {
        _imageManager.RemoveAll();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        App.Current.ExitApplication();
    }

    private void HubWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (App.Current.IsExiting)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void HubWindow_Closed(object? sender, EventArgs e)
    {
        _hotkeyService?.Dispose();
    }

    private void ChangeImage(ImageItem item)
    {
        var filePath = SelectImageFiles().FirstOrDefault();
        if (filePath is null)
        {
            return;
        }

        try
        {
            _imageManager.ChangeImage(item, filePath);
        }
        catch (Exception ex)
        {
            ShowImageError(ex);
        }
    }

    private IReadOnlyList<string> SelectImageFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select images to display",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        return dialog.ShowDialog(this) == true ? dialog.FileNames : [];
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

    private void ShowImageError(Exception exception)
    {
        MessageBox.Show(
            this,
            $"Could not load the image.\n\n{exception.Message}",
            "Image Loading Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
