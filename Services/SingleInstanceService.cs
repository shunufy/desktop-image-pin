namespace DesktopImagePin.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string DefaultMutexName = @"Local\DesktopImagePin.Instance";
    private const string DefaultActivationEventName = @"Local\DesktopImagePin.Activate";

    private readonly string _mutexName;
    private readonly string _activationEventName;
    private Mutex? _mutex;
    private EventWaitHandle? _activationEvent;
    private RegisteredWaitHandle? _registeredWait;
    private bool _ownsMutex;

    public SingleInstanceService(
        string mutexName = DefaultMutexName,
        string activationEventName = DefaultActivationEventName)
    {
        _mutexName = mutexName;
        _activationEventName = activationEventName;
    }

    public bool TryAcquire(Action activationRequested)
    {
        ArgumentNullException.ThrowIfNull(activationRequested);

        _mutex = new Mutex(initiallyOwned: true, _mutexName, out var createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            SignalExistingInstance();
            return false;
        }

        _ownsMutex = true;
        _activationEvent = new EventWaitHandle(
            initialState: false,
            EventResetMode.AutoReset,
            _activationEventName);
        _registeredWait = ThreadPool.RegisterWaitForSingleObject(
            _activationEvent,
            (_, timedOut) =>
            {
                if (!timedOut)
                {
                    activationRequested();
                }
            },
            state: null,
            Timeout.Infinite,
            executeOnlyOnce: false);

        return true;
    }

    private void SignalExistingInstance()
    {
        try
        {
            using var activationEvent = EventWaitHandle.OpenExisting(_activationEventName);
            activationEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // The first instance may still be starting. The duplicate still exits safely.
        }
    }

    public void Dispose()
    {
        _registeredWait?.Unregister(null);
        _registeredWait = null;
        _activationEvent?.Dispose();
        _activationEvent = null;

        if (_ownsMutex)
        {
            try
            {
                _mutex?.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // The mutex was already released during shutdown.
            }
        }

        _ownsMutex = false;
        _mutex?.Dispose();
        _mutex = null;
    }
}
