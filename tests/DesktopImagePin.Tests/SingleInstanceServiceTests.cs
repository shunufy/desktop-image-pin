using DesktopImagePin.Services;

namespace DesktopImagePin.Tests;

public sealed class SingleInstanceServiceTests
{
    [Fact]
    public void SecondInstance_SignalsFirstInstanceAndDoesNotAcquire()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var mutexName = $@"Local\DesktopImagePin.Tests.{suffix}";
        var eventName = $@"Local\DesktopImagePin.Tests.Activate.{suffix}";
        using var activationReceived = new ManualResetEventSlim();
        using var first = new SingleInstanceService(mutexName, eventName);
        using var second = new SingleInstanceService(mutexName, eventName);

        Assert.True(first.TryAcquire(activationReceived.Set));
        Assert.False(second.TryAcquire(() => { }));
        Assert.True(activationReceived.Wait(TimeSpan.FromSeconds(2)));
    }
}
