using System.Windows;
using System.Windows.Threading;
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
    private SingleInstanceService? _singleInstanceService;
    private DispatcherTimer? _autosaveTimer;
    private bool _autosaveErrorShown;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceService = new SingleInstanceService();
        if (!_singleInstanceService.TryAcquire(
                () => Dispatcher.BeginInvoke(
                    () =>
                    {
                        if (HubWindow is not null)
                        {
                            ShowHubWindow();
                        }
                    })))
        {
            Shutdown();
            return;
        }

        _imageStateStore = new ImageStateStore();
        ImageManager = new ImageManager();
        ImageManager.StateChanged += ImageManager_StateChanged;
        _autosaveTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(1500)
        };
        _autosaveTimer.Tick += AutosaveTimer_Tick;
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
        _autosaveTimer?.Stop();
        SaveImageState(showErrors: true);
        _trayIconService?.Dispose();
        _trayIconService = null;
        ImageManager.RemoveAll();
        HubWindow.Close();
        Shutdown();
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        _autosaveTimer?.Stop();
        SaveImageState(showErrors: true);
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _autosaveTimer?.Stop();
        if (ImageManager is not null)
        {
            ImageManager.StateChanged -= ImageManager_StateChanged;
        }

        _trayIconService?.Dispose();
        _singleInstanceService?.Dispose();
        base.OnExit(e);
    }

    private void ImageManager_StateChanged(object? sender, EventArgs e)
    {
        if (IsExiting || _autosaveTimer is null)
        {
            return;
        }

        _autosaveTimer.Stop();
        _autosaveTimer.Start();
    }

    private void AutosaveTimer_Tick(object? sender, EventArgs e)
    {
        _autosaveTimer?.Stop();
        SaveImageState(showErrors: !_autosaveErrorShown);
    }

    private void SaveImageState(bool showErrors)
    {
        try
        {
            _imageStateStore.Save(ImageManager.Items);
            _autosaveErrorShown = false;
        }
        catch (Exception ex)
        {
            if (showErrors)
            {
                _autosaveErrorShown = true;
                MessageBox.Show(
                    $"Could not save the image layout.\n\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
