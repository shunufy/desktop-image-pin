using System.Windows;
using DesktopImagePin.Services;
using DesktopImagePin.Windows;
using MessageBox = System.Windows.MessageBox;

namespace DesktopImagePin;

public partial class App : System.Windows.Application
{
    public static new App Current => (App)System.Windows.Application.Current;

    public ImageManager ImageManager { get; private set; } = null!;
    public HubWindow HubWindow { get; private set; } = null!;
    public bool IsExiting { get; private set; }
    private ImageStateStore _imageStateStore = null!;
    private TrayIconService? _trayIconService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _imageStateStore = new ImageStateStore();
        ImageManager = new ImageManager();
        HubWindow = new HubWindow(ImageManager);
        MainWindow = HubWindow;

        _trayIconService = new TrayIconService(
            ShowHubWindow,
            () =>
            {
                ShowHubWindow();
                HubWindow.ShowAddImageDialog();
            },
            ExitApplication);

        HubWindow.Show();
        ImageManager.RestoreImages(_imageStateStore.Load());
    }

    public void ToggleHubWindow()
    {
        if (HubWindow.IsVisible)
        {
            HubWindow.Hide();
            return;
        }

        ShowHubWindow();
    }

    public void ShowHubWindow()
    {
        if (HubWindow.WindowState == WindowState.Minimized)
        {
            HubWindow.WindowState = WindowState.Normal;
        }

        HubWindow.Show();
        HubWindow.Activate();
    }

    public void ExitApplication()
    {
        if (IsExiting)
        {
            return;
        }

        IsExiting = true;
        SaveImageState();
        _trayIconService?.Dispose();
        _trayIconService = null;
        ImageManager.RemoveAll();
        HubWindow.Close();
        Shutdown();
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        SaveImageState();
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        base.OnExit(e);
    }

    private void SaveImageState()
    {
        try
        {
            _imageStateStore.Save(ImageManager.Items);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not save the image layout.\n\n{ex.Message}",
                "Save Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
