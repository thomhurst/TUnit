using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._2067;

public class DataClass :
    IAsyncInitializer,
    ITestRegisteredEventReceiver,
    ITestStartEventReceiver,
    ITestEndEventReceiver,
    IAsyncDisposable
{
    public bool IsRegistered { get; set; }
    public bool IsInitialized { get; set; }
    public bool IsStarted { get; set; }
    public bool IsEnded { get; set; }
    public bool IsDisposed { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        IsRegistered = true;
        return default(ValueTask);
    }


    public ValueTask OnTestStart(TestContext beforeTestContext)
    {
        IsStarted = true;
        return default(ValueTask);
    }


    public ValueTask OnTestEnd(TestContext testContext)
    {
        IsEnded = true;
        return default(ValueTask);
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default(ValueTask);
    }

    public int Order => 0;
}
