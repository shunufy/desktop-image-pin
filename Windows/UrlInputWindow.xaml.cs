using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Clipboard = System.Windows.Clipboard;

namespace DesktopImagePin.Windows;

public partial class UrlInputWindow : Window
{
    public UrlInputWindow()
    {
        InitializeComponent();

        if (Clipboard.ContainsText())
        {
            var clipboardText = Clipboard.GetText().Trim();
            if (Uri.TryCreate(clipboardText, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                UrlTextBox.Text = clipboardText;
                UrlTextBox.SelectAll();
            }
        }

        Loaded += (_, _) => UrlTextBox.Focus();
    }

    public string ImageUrl => UrlTextBox.Text.Trim();

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ImageUrl))
        {
            return;
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(ImageUrl))
        {
            DialogResult = true;
        }
    }
}
