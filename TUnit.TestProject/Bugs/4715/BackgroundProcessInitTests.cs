using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4715;

/// <summary>
/// Reproduction for issue #4715: InitializeAsync starts a background process
/// (simulating Aspire DistributedApplication.StartAsync), then throws.
/// The background process keeps running and prevents the process from exiting.
/// </summary>

public class BackgroundProcessHost4715 : IAsyncDiscoveryInitializer, IAsyncDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _backgroundTask;

    public async Task InitializeAsync()
    {
        // Simulate starting a background process (like Aspire's Application.StartAsync())
        _cts = new CancellationTokenSource();
        _backgroundTask = Task.Run(async () =>
        {
            try
            {
                // Simulate a long-running service
                await Task.Delay(Timeout.Infinite, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when disposed
            }
        }, _cts.Token);

        // Give the background task time to start
        await Task.Delay(10);

        // Now throw (simulating WaitForResourceAsync timeout)
        throw new TimeoutException("The operation has timed out waiting for resource.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }

        if (_backgroundTask != null)
        {
            try
            {
                await _backgroundTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }
}

public class BackgroundFactory4715 : IAsyncDiscoveryInitializer
{
    [ClassDataSource<BackgroundProcessHost4715>(Shared = SharedType.PerTestSession)]
    public required BackgroundProcessHost4715 AppHost { get; init; }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}

[EngineTest(ExpectedResult.Failure)]
public class BackgroundProcessInitFailureTests
{
    [ClassDataSource<BackgroundFactory4715>(Shared = SharedType.PerTestSession)]
    public required BackgroundFactory4715 Factory { get; init; }

    [Test]
    public void Test_Should_Fail_Not_Stall()
    {
        throw new InvalidOperationException("This test should not have executed");
    }
}
