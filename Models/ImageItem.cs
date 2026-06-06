using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using DesktopImagePin.Windows;

namespace DesktopImagePin.Models;

public sealed class ImageItem : INotifyPropertyChanged
{
    private string _filePath;
    private double _scale = 1.0;
    private double _scaleX = 1.0;
    private double _scaleY = 1.0;
    private ImageDisplayLayer _displayLayer = ImageDisplayLayer.Normal;
    private double? _left;
    private double? _top;

    public ImageItem(string filePath)
    {
        Id = Guid.NewGuid();
        _filePath = filePath;
    }

    public Guid Id { get; }

    public string FilePath
    {
        get => _filePath;
        internal set
        {
            if (_filePath == value)
            {
                return;
            }

            _filePath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string DisplayName => Path.GetFileName(FilePath);

    public double Scale
    {
        get => _scale;
        internal set
        {
            if (Math.Abs(_scale - value) < 0.0001)
            {
                return;
            }

            _scale = value;
            OnPropertyChanged();
        }
    }

    public double ScaleX
    {
        get => _scaleX;
        internal set
        {
            if (Math.Abs(_scaleX - value) < 0.0001)
            {
                return;
            }

            _scaleX = value;
            OnPropertyChanged();
        }
    }

    public double ScaleY
    {
        get => _scaleY;
        internal set
        {
            if (Math.Abs(_scaleY - value) < 0.0001)
            {
                return;
            }

            _scaleY = value;
            OnPropertyChanged();
        }
    }

    public ImageDisplayLayer DisplayLayer
    {
        get => _displayLayer;
        internal set
        {
            if (_displayLayer == value)
            {
                return;
            }

            _displayLayer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayLayerText));
        }
    }

    public double? Left
    {
        get => _left;
        internal set
        {
            if (_left == value)
            {
                return;
            }

            _left = value;
            OnPropertyChanged();
        }
    }

    public double? Top
    {
        get => _top;
        internal set
        {
            if (_top == value)
            {
                return;
            }

            _top = value;
            OnPropertyChanged();
        }
    }

    public string DisplayLayerText => DisplayLayer switch
    {
        ImageDisplayLayer.Topmost => "Always on Top",
        ImageDisplayLayer.Bottommost => "Back",
        _ => "Normal"
    };

    public ImageWindow? Window { get; internal set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
