using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace DesktopImagePin.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _contextMenu;
    private readonly Icon _icon;
    private bool _disposed;

    public TrayIconService(Action showHub, Action addImages, Action exitApplication)
    {
        _icon = LoadApplicationIcon();
        _contextMenu = new Forms.ContextMenuStrip();
        _contextMenu.Items.Add("Show Hub", null, (_, _) => Dispatch(showHub));
        _contextMenu.Items.Add("Add Images", null, (_, _) => Dispatch(addImages));
        _contextMenu.Items.Add(new Forms.ToolStripSeparator());
        _contextMenu.Items.Add("Exit", null, (_, _) => Dispatch(exitApplication));

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _icon,
            Text = "Desktop Image Pin",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => Dispatch(showHub);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _icon.Dispose();
        _disposed = true;
    }

    private static void Dispatch(Action action)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(action);
    }

    private static Icon LoadApplicationIcon()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            var extractedIcon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);
            if (extractedIcon is not null)
            {
                return extractedIcon;
            }
        }

        return (Icon)SystemIcons.Application.Clone();
    }
}
