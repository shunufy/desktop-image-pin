using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopImagePin.Models;
using DesktopImagePin.Services;
using DesktopImagePin.Windows;

namespace DesktopImagePin.Tests;

public sealed class HubWindowTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"DesktopImagePin.HubTests-{Guid.NewGuid():N}");

    [Fact]
    public void RecyclingRows_DoesNotChangeImageOpacityOrRequestSaving()
    {
        RunWithHub(1000, (hub, manager, root) =>
        {
            var expected = manager.Items.Select(item => item.Opacity).ToArray();
            var changes = 0;
            manager.StateChanged += (_, _) => changes++;
            var list = (ListBox)hub.FindName("ImagesListBox");
            var scrollViewer = Descendants<ScrollViewer>(list).First();

            foreach (var index in new[] { 0, 100, 500, 999, 0 })
            {
                scrollViewer.ScrollToVerticalOffset(index);
                Layout(root);
                Assert.NotNull(list.ItemContainerGenerator.ContainerFromIndex(index));
                Assert.InRange(Descendants<ListBoxItem>(list).Count(), 1, 30);
            }

            Assert.Equal(expected, manager.Items.Select(item => item.Opacity).ToArray());
            Assert.Equal(0, changes);
        });
    }

    [Theory]
    [InlineData(900)]
    [InlineData(1120)]
    public void MinimumHubSize_KeepsRowActionsInsideTheScrollableViewport(double width)
    {
        RunWithHub(10, (hub, _, root) =>
        {
            Layout(root, width);
            var list = (ListBox)hub.FindName("ImagesListBox");
            var viewport = Descendants<ScrollContentPresenter>(list).First();
            var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
            var actions = Descendants<Button>(row).ToArray();
            Assert.Contains(actions, button => Equals(button.Content, "Remove"));

            foreach (var action in actions)
            {
                var bounds = action.TransformToAncestor(viewport)
                    .TransformBounds(new Rect(action.RenderSize));
                Assert.True(bounds.Left >= 0 && bounds.Right <= viewport.ActualWidth + 0.5,
                    $"{action.Content}: {bounds}, viewport width {viewport.ActualWidth}");
            }
        });
    }

    [Fact]
    public void SelectionControls_FollowGroupingChangesFromImageWindows()
    {
        RunWithHub(2, (hub, manager, _) =>
        {
            var list = (ListBox)hub.FindName("ImagesListBox");
            var ungroup = (Button)hub.FindName("UngroupSelectedButton");
            manager.GroupImages(manager.Items);
            list.SelectedItem = manager.Items[0];
            Assert.True(ungroup.IsEnabled);
            manager.UngroupImages([manager.Items[0]]);
            Assert.False(ungroup.IsEnabled);
            manager.GroupImages(manager.Items);
            Assert.True(ungroup.IsEnabled);
            manager.RemoveImage(manager.Items[1]);
            Assert.False(ungroup.IsEnabled);
        });
    }

    [Fact]
    public void LargeSelection_GroupUngroupAndRemovalKeepSelectionControlsCurrent()
    {
        RunWithHub(1000, (hub, manager, _) =>
        {
            var list = (ListBox)hub.FindName("ImagesListBox");
            var group = (Button)hub.FindName("GroupSelectedButton");
            var ungroup = (Button)hub.FindName("UngroupSelectedButton");
            var count = (TextBlock)hub.FindName("SelectedCountTextBlock");
            list.SelectAll();
            Assert.True(group.IsEnabled);
            Assert.False(ungroup.IsEnabled);
            Assert.Equal("Selected 1000", count.Text);

            group.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.True(ungroup.IsEnabled);
            Assert.NotNull(Assert.Single(manager.Items.Select(item => item.GroupId).Distinct()));
            ungroup.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.False(ungroup.IsEnabled);
            Assert.All(manager.Items, item => Assert.False(item.IsGrouped));

            group.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            list.UnselectAll();
            Assert.False(ungroup.IsEnabled);
            manager.UngroupImages([manager.Items[0]]);
            manager.GroupImages(manager.Items);
            Assert.False(ungroup.IsEnabled);

            list.SelectedItem = manager.Items[0];
            Assert.True(ungroup.IsEnabled);
            manager.RemoveImage(manager.Items[0]);
            Assert.False(ungroup.IsEnabled);
            Assert.False(group.IsEnabled);
            Assert.Equal("Selected 0", count.Text);
        });
    }

    [Fact]
    public void OpacitySlider_UpdatesOnlyItsImageAndPreservesItsBinding()
    {
        RunWithHub(2, (hub, manager, root) =>
        {
            var list = (ListBox)hub.FindName("ImagesListBox");
            var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
            var slider = Assert.Single(Descendants<Slider>(row));
            var otherOpacity = manager.Items[1].Opacity;
            slider.SetCurrentValue(RangeBase.ValueProperty, 45.0);
            Layout(root);

            Assert.Equal(0.45, manager.Items[0].Opacity, precision: 8);
            Assert.Equal(otherOpacity, manager.Items[1].Opacity);
            manager.SetOpacity(manager.Items[0], 0.7);
            Layout(root);
            Assert.Equal(70, slider.Value, precision: 8);
        });
    }

    private void RunWithHub(int count, Action<HubWindow, ImageManager, FrameworkElement> action)
    {
        WpfTestHelper.Run(() =>
        {
            var manager = new ImageManager(() => [new Rect(0, 0, 1920, 1040)], _ => { });
            for (var index = 0; index < count; index++)
            {
                manager.Items.Add(new ImageItem(Path.Combine(_directory, $"image-{index:D4}.png"))
                {
                    Opacity = 0.2 + (index % 8) * 0.1
                });
            }

            var startup = new StartupService(new IsolatedStartupStore(), () => Path.Combine(_directory, "app.exe"));
            var hub = new HubWindow(manager, new ImageImportService(Path.Combine(_directory, "imports")),
                startup, Path.Combine(_directory, "url-imports.json"));
            try
            {
                // Measure the real Hub content without showing a window, registering a
                // hotkey, starting App, or consulting the user's library/registry.
                var root = (FrameworkElement)hub.Content;
                hub.Content = null;
                root.DataContext = manager;
                Layout(root);
                action(hub, manager, root);
                Assert.False(hub.IsVisible);
                Assert.Equal(IntPtr.Zero, new WindowInteropHelper(hub).Handle);
            }
            finally
            {
                hub.Close();
                manager.RemoveAll();
            }
        });
    }

    private static void Layout(FrameworkElement root, double width = 1120)
    {
        // Reserve the native window frame; all dimensions are WPF logical units.
        var size = new Size(width - 16, 500 - 40);
        root.Measure(size);
        root.Arrange(new Rect(size));
        root.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        root.UpdateLayout();
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in Descendants<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class IsolatedStartupStore : IStartupRegistryStore
    {
        public string? GetValue(string name) => null;
        public void SetValue(string name, string value) => throw new InvalidOperationException("Registry writes are not part of Hub tests.");
        public void DeleteValue(string name) => throw new InvalidOperationException("Registry writes are not part of Hub tests.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
