using DesktopImagePin.Services;

namespace DesktopImagePin.Tests;

public sealed class StartupServiceTests
{
    [Fact]
    public void BuildStartupCommand_QuotesExecutablePath()
    {
        var command = StartupService.BuildStartupCommand(@"C:\Program Files\Desktop Image Pin\DesktopImagePin.exe");

        Assert.Equal("\"C:\\Program Files\\Desktop Image Pin\\DesktopImagePin.exe\"", command);
    }

    [Fact]
    public void SetEnabled_AddsCurrentExecutableToRunEntry()
    {
        var store = new InMemoryStartupRegistryStore();
        var service = new StartupService(store, () => @"C:\Apps\DesktopImagePin.exe");

        service.SetEnabled(true);

        Assert.True(service.IsEnabled());
        Assert.Equal("\"C:\\Apps\\DesktopImagePin.exe\"", store.Values["DesktopImagePin"]);
    }

    [Fact]
    public void SetEnabledFalse_RemovesRunEntry()
    {
        var store = new InMemoryStartupRegistryStore();
        var service = new StartupService(store, () => @"C:\Apps\DesktopImagePin.exe");

        service.SetEnabled(true);
        service.SetEnabled(false);

        Assert.False(service.IsEnabled());
        Assert.False(store.Values.ContainsKey("DesktopImagePin"));
    }

    [Fact]
    public void IsEnabled_ReturnsFalseForStaleExecutablePath()
    {
        var store = new InMemoryStartupRegistryStore();
        store.Values["DesktopImagePin"] = "\"C:\\Old\\DesktopImagePin.exe\"";
        var service = new StartupService(store, () => @"C:\Apps\DesktopImagePin.exe");

        Assert.False(service.IsEnabled());
    }

    private sealed class InMemoryStartupRegistryStore : IStartupRegistryStore
    {
        public Dictionary<string, string> Values { get; } = [];

        public string? GetValue(string name)
        {
            return Values.TryGetValue(name, out var value) ? value : null;
        }

        public void SetValue(string name, string value)
        {
            Values[name] = value;
        }

        public void DeleteValue(string name)
        {
            Values.Remove(name);
        }
    }
}
