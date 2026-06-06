using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DesktopImagePin.Services;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Windows = 0x0008,
    NoRepeat = 0x4000
}

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0x4842;

    private readonly IntPtr _windowHandle;
    private readonly HwndSource _source;
    private readonly Action _callback;
    private bool _disposed;

    public GlobalHotkeyService(
        Window window,
        HotkeyModifiers modifiers,
        Key key,
        Action callback)
    {
        _callback = callback;
        _windowHandle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_windowHandle)
            ?? throw new InvalidOperationException("Could not obtain the window handle.");

        _source.AddHook(WindowProcedure);

        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (!RegisterHotKey(_windowHandle, HotkeyId, (uint)modifiers, virtualKey))
        {
            _source.RemoveHook(WindowProcedure);
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Could not register the global hotkey.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnregisterHotKey(_windowHandle, HotkeyId);
        _source.RemoveHook(WindowProcedure);
        _disposed = true;
    }

    private IntPtr WindowProcedure(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            _callback();
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(
        IntPtr windowHandle,
        int id,
        uint modifiers,
        uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);
}
