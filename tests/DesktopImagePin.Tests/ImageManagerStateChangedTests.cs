using DesktopImagePin.Models;
using DesktopImagePin.Services;

namespace DesktopImagePin.Tests;

public sealed class ImageManagerStateChangedTests
{
    [Fact]
    public void ItemPropertyChange_RaisesStateChangedForAutosave()
    {
        var manager = new ImageManager();
        var item = new ImageItem(@"C:\images\sample.png");
        manager.Items.Add(item);
        var changeCount = 0;
        manager.StateChanged += (_, _) => changeCount++;

        item.Left = 123;

        Assert.Equal(1, changeCount);
    }
}
