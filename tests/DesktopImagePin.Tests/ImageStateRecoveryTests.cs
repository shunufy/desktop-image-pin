using DesktopImagePin.Models;
using DesktopImagePin.Services;

namespace DesktopImagePin.Tests;

public sealed class ImageStateRecoveryTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"DesktopImagePin.Local.Tests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndLoad_RoundTripsGroupAndPlacement()
    {
        Directory.CreateDirectory(_directory);
        var statePath = Path.Combine(_directory, "images.json");
        var groupId = Guid.NewGuid();
        var item = new ImageItem(@"C:\images\sample.png")
        {
            Scale = 0.75,
            ScaleX = 1.2,
            ScaleY = 0.8,
            Left = 120,
            Top = 240,
            GroupId = groupId
        };
        var store = new ImageStateStore(statePath);

        store.Save([item]);
        var restored = Assert.Single(store.Load());

        Assert.Equal(item.FilePath, restored.FilePath);
        Assert.Equal(1.2, restored.ScaleX);
        Assert.Equal(0.8, restored.ScaleY);
        Assert.Equal(120, restored.Left);
        Assert.Equal(240, restored.Top);
        Assert.Equal(groupId, restored.GroupId);
    }

    [Fact]
    public void Load_UsesPreviousBackupWhenPrimaryIsMalformed()
    {
        Directory.CreateDirectory(_directory);
        var statePath = Path.Combine(_directory, "images.json");
        var store = new ImageStateStore(statePath);

        store.Save([new ImageItem(@"C:\images\first.png")]);
        store.Save([new ImageItem(@"C:\images\second.png")]);
        File.WriteAllText(statePath, "{not-json");

        var restored = Assert.Single(store.Load());

        Assert.Equal(@"C:\images\first.png", restored.FilePath);
    }

    [Fact]
    public void SaveAfterRecovery_PreservesValidBackupAcrossRepeatedCorruption()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "images.json");
        var backup = Path.Combine(_directory, "images.backup.json");
        var store = new ImageStateStore(path);
        store.Save([new ImageItem(@"C:\images\first.png")]);
        store.Save([new ImageItem(@"C:\images\second.png")]);
        var validBackup = File.ReadAllText(backup);
        File.WriteAllText(path, "{broken");

        var recovered = Assert.Single(store.Load());
        store.Save([new ImageItem(recovered.FilePath)]);

        Assert.Equal(validBackup, File.ReadAllText(backup));
        File.WriteAllText(path, "{broken-again");
        Assert.Equal(recovered.FilePath, Assert.Single(store.Load()).FilePath);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{}]")]
    [InlineData("[{\"FilePath\":null}]")]
    [InlineData("[{\"FilePath\":\" \"}]")]
    [InlineData("[{\"FilePath\":\"sample.png\",\"Left\":1e309}]")]
    [InlineData("[{\"FilePath\":\"sample.png\",\"ScaleX\":0}]")]
    [InlineData("[{\"FilePath\":\"sample.png\",\"Opacity\":2}]")]
    [InlineData("[{\"FilePath\":\"sample.png\",\"RotationDegrees\":45}]")]
    [InlineData("[{\"FilePath\":\"sample.png\",\"DisplayLayer\":99}]")]
    public void InvalidStateContents_UseBackupAndDoNotOverwriteItOnSave(string invalidJson)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "images.json");
        var backup = Path.Combine(_directory, "images.backup.json");
        var store = new ImageStateStore(path);
        store.Save([new ImageItem(@"C:\images\valid.png")]);
        store.Save([new ImageItem(@"C:\images\next.png")]);
        var backupJson = File.ReadAllText(backup);
        File.WriteAllText(path, invalidJson);

        Assert.Equal(@"C:\images\valid.png", Assert.Single(store.Load()).FilePath);
        store.Save([new ImageItem(@"C:\images\valid.png")]);
        Assert.Equal(backupJson, File.ReadAllText(backup));
    }

    [Fact]
    public void LegacyState_DefaultsStillLoad()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "images.json");
        File.WriteAllText(path, "[{\"FilePath\":\"legacy.png\",\"Scale\":0.5}]");

        var state = Assert.Single(new ImageStateStore(path).Load());
        Assert.Equal(0.5, state.Scale);
        Assert.Null(state.ScaleX);
        Assert.Null(state.GroupId);
    }

    [Fact]
    public void InvalidSave_LeavesPrimaryAndBackupUnchanged()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "images.json");
        var backup = Path.Combine(_directory, "images.backup.json");
        var store = new ImageStateStore(path);
        store.Save([new ImageItem("valid.png")]);
        store.Save([new ImageItem("next.png")]);
        var primaryBefore = File.ReadAllText(path);
        var backupBefore = File.ReadAllText(backup);

        Assert.Throws<InvalidDataException>(() => store.Save(
            [new ImageItem("invalid.png") { Left = double.NaN }]));
        Assert.Equal(primaryBefore, File.ReadAllText(path));
        Assert.Equal(backupBefore, File.ReadAllText(backup));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
