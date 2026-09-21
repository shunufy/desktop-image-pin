using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopImagePin.Models;
using DesktopImagePin.Services;
using Microsoft.Win32;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using IDataObject = System.Windows.IDataObject;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Slider = System.Windows.Controls.Slider;

namespace DesktopImagePin.Windows;

public partial class HubWindow : Window
{
    private readonly ImageManager _imageManager;
    private readonly ImageImportService _imageImportService = new();
    private readonly StartupService _startupService = new();
    private readonly UrlImportLibrary _urlImportLibrary;
    private GlobalHotkeyService? _hotkeyService;

    public HubWindow(ImageManager imageManager)
    {
        InitializeComponent();

        _imageManager = imageManager;
        _urlImportLibrary = new UrlImportLibrary(_imageImportService);
        DataContext = imageManager;
        UrlImportsListBox.ItemsSource = _urlImportLibrary.Items;
        RefreshStartupCheckBox();

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

    private void StartupCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox)
        {
            return;
        }

        try
        {
            _startupService.SetEnabled(checkBox.IsChecked == true);
            checkBox.IsChecked = _startupService.IsEnabled();
        }
        catch (Exception ex)
        {
            RefreshStartupCheckBox();
            MessageBox.Show(
                this,
                $"Could not update the Windows startup setting.\n\n{ex.Message}",
                "Startup Setting Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public void ShowAddImageDialog()
    {
        var wasEmpty = _imageManager.Items.Count == 0;
        var filePaths = SelectImageFiles();
        if (filePaths.Length == 0)
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
            if (!_imageManager.TryDuplicateImage(item, out var error))
            {
                ShowImageError(error!);
            }
        }
    }

    private void RetryImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ImageItem item })
        {
            return;
        }

        try
        {
            _imageManager.RetryImage(item);
        }
        catch (Exception exception)
        {
            ShowImageError(exception);
        }
    }

    private void GroupSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        _imageManager.GroupImages(ImagesListBox.SelectedItems.Cast<ImageItem>().ToArray());
        RefreshGroupControls();
    }

    private void UngroupSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        _imageManager.UngroupImages(ImagesListBox.SelectedItems.Cast<ImageItem>().ToArray());
        RefreshGroupControls();
    }

    private void BringOffscreenImagesButton_Click(object sender, RoutedEventArgs e)
    {
        var movedCount = _imageManager.BringOffscreenImagesIntoView();
        MessageBox.Show(this,
            movedCount == 0
                ? "No off-screen images were found."
                : $"Moved {movedCount} off-screen images back into view.",
            "Image Placement", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ImagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshGroupControls();
    }

    private void RefreshGroupControls()
    {
        var selectedItems = ImagesListBox.SelectedItems.Cast<ImageItem>().ToArray();
        SelectedCountTextBlock.Text = $"Selected {selectedItems.Length}";
        GroupSelectedButton.IsEnabled = selectedItems.Length >= 2;
        UngroupSelectedButton.IsEnabled = selectedItems.Any(item => item.IsGrouped);
    }

    private void AddClipboardImageButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var filePath = _imageImportService.ImportClipboardImage();
            if (filePath is null)
            {
                MessageBox.Show(
                    this,
                    "The clipboard does not contain an image or image file.",
                    "Clipboard",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            _imageManager.AddImage(filePath);
        }
        catch (Exception ex)
        {
            ShowImageError(ex);
        }
    }

    private async void AddImageFromUrlButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new UrlInputWindow
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var filePath = await _imageImportService.ImportFromUrlAsync(dialog.ImageUrl);
            _imageManager.AddImage(filePath);
        }
        catch (Exception ex)
        {
            ShowImageError(ex);
        }
    }

    private async void SaveUrlImportButton_Click(object sender, RoutedEventArgs e)
    {
        SaveUrlImportButton.IsEnabled = false;

        try
        {
            var savedItem = await _urlImportLibrary.SaveAsync(
                ImportNameTextBox.Text,
                ImportUrlTextBox.Text);

            UrlImportsListBox.SelectedItem = savedItem;
            UrlImportsListBox.ScrollIntoView(savedItem);
            ImportUrlTextBox.Clear();
            ImportNameTextBox.Clear();
        }
        catch (Exception ex)
        {
            ShowImageError(ex);
        }
        finally
        {
            SaveUrlImportButton.IsEnabled = true;
        }
    }

    private void DisplayUrlImportButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not SavedUrlImport item)
        {
            return;
        }

        if (!File.Exists(item.LocalFilePath))
        {
            MessageBox.Show(
                this,
                "The cached image file is missing. Refresh this entry to download it again.",
                "Cached Image Missing",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            _imageManager.AddImage(item.LocalFilePath);
        }
        catch (Exception ex)
        {
            ShowImageError(ex);
        }
    }

    private async void RefreshUrlImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: SavedUrlImport item } button)
        {
            return;
        }

        button.IsEnabled = false;

        try
        {
            await _urlImportLibrary.RefreshAsync(item);
        }
        catch (Exception ex)
        {
            ShowImageError(ex);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private void DeleteUrlImportButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not SavedUrlImport item)
        {
            return;
        }

        var isDisplayed = _imageManager.Items.Any(
            image => string.Equals(
                image.FilePath,
                item.LocalFilePath,
                StringComparison.OrdinalIgnoreCase));

        _urlImportLibrary.Delete(item, deleteCachedFile: !isDisplayed);
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

    private void RotateLeftButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ImageItem item)
        {
            _imageManager.RotateImage(item, -90);
        }
    }

    private void RotateRightButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ImageItem item)
        {
            _imageManager.RotateImage(item, 90);
        }
    }

    private void FlipHorizontalButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ImageItem item)
        {
            _imageManager.ToggleHorizontalFlip(item);
        }
    }

    private void FlipVerticalButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ImageItem item)
        {
            _imageManager.ToggleVerticalFlip(item);
        }
    }

    private void ClickThroughCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { Tag: ImageItem item } checkBox)
        {
            _imageManager.SetClickThrough(item, checkBox.IsChecked == true);
        }
    }

    private void OpacitySlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is Slider { Tag: ImageItem item })
        {
            _imageManager.SetOpacity(item, e.NewValue / 100.0);
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
        var filePaths = SelectImageFiles();
        if (filePaths.Length == 0)
        {
            return;
        }

        try
        {
            _imageManager.ChangeImage(item, filePaths[0]);
        }
        catch (Exception ex)
        {
            ShowImageError(ex);
        }
    }

    private string[] SelectImageFiles()
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

    private void RefreshStartupCheckBox()
    {
        try
        {
            StartupCheckBox.IsChecked = _startupService.IsEnabled();
        }
        catch
        {
            StartupCheckBox.IsChecked = false;
        }
    }
}
