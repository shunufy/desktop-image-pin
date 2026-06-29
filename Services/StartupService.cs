using Microsoft.Win32;

namespace DesktopImagePin.Services;

public sealed class StartupService
{
    private const string ApplicationName = "DesktopImagePin";
    private readonly IStartupRegistryStore _registryStore;
    private readonly Func<string?> _getExecutablePath;

    public StartupService()
        : this(new StartupRegistryStore(), () => Environment.ProcessPath)
    {
    }

    internal StartupService(IStartupRegistryStore registryStore, Func<string?> getExecutablePath)
    {
        _registryStore = registryStore;
        _getExecutablePath = getExecutablePath;
    }

    public bool IsEnabled()
    {
        var expectedCommand = GetStartupCommand();
        var currentCommand = _registryStore.GetValue(ApplicationName);

        return string.Equals(currentCommand, expectedCommand, StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool isEnabled)
    {
        if (isEnabled)
        {
            _registryStore.SetValue(ApplicationName, GetStartupCommand());
            return;
        }

        _registryStore.DeleteValue(ApplicationName);
    }

    internal static string BuildStartupCommand(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("The application executable path could not be resolved.");
        }

        return $"\"{executablePath}\"";
    }

    private string GetStartupCommand()
    {
        var executablePath = _getExecutablePath();
        return BuildStartupCommand(executablePath ?? string.Empty);
    }
}

internal interface IStartupRegistryStore
{
    string? GetValue(string name);

    void SetValue(string name, string value);

    void DeleteValue(string name);
}

internal sealed class StartupRegistryStore : IStartupRegistryStore
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public string? GetValue(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(name) as string;
    }

    public void SetValue(string name, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        key.SetValue(name, value, RegistryValueKind.String);
    }

    public void DeleteValue(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
