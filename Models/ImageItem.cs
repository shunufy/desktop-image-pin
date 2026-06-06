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
    private double _opacity = 1.0;
    private int _rotationDegrees;
    private bool _flipHorizontal;
    private bool _flipVertical;
    private bool _isClickThrough;

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

    public double Opacity
    {
        get => _opacity;
        internal set
        {
            if (Math.Abs(_opacity - value) < 0.0001)
            {
                return;
            }

            _opacity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OpacityPercent));
        }
    }

    public int OpacityPercent => (int)Math.Round(Opacity * 100);

    public int RotationDegrees
    {
        get => _rotationDegrees;
        internal set
        {
            var normalized = ((value % 360) + 360) % 360;
            if (_rotationDegrees == normalized)
            {
                return;
            }

            _rotationDegrees = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TransformText));
        }
    }

    public bool FlipHorizontal
    {
        get => _flipHorizontal;
        internal set
        {
            if (_flipHorizontal == value)
            {
                return;
            }

            _flipHorizontal = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TransformText));
        }
    }

    public bool FlipVertical
    {
        get => _flipVertical;
        internal set
        {
            if (_flipVertical == value)
            {
                return;
            }

            _flipVertical = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TransformText));
        }
    }

    public bool IsClickThrough
    {
        get => _isClickThrough;
        internal set
        {
            if (_isClickThrough == value)
            {
                return;
            }

            _isClickThrough = value;
            OnPropertyChanged();
        }
    }

    public string TransformText
    {
        get
        {
            var parts = new List<string>();
            if (RotationDegrees != 0)
            {
                parts.Add($"{RotationDegrees}°");
            }

            if (FlipHorizontal)
            {
                parts.Add("Flip H");
            }

            if (FlipVertical)
            {
                parts.Add("Flip V");
            }

            return parts.Count == 0 ? "No transform" : string.Join(" / ", parts);
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
