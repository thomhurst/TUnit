using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// End-to-end regression test for https://github.com/thomhurst/TUnit/issues/6339.
/// <para>
/// With a minimal-hosting SUT, disposing the per-test factory races the SUT's own
/// <c>app.Run()</c> shutdown path: <c>WebApplicationFactory.DisposeAsync</c> calls
/// <c>Host.StopAsync</c>, whose <c>ApplicationStopping</c> signal wakes the app's parked
/// <c>WaitForShutdownAsync</c>, which calls <c>Host.StopAsync</c> again — concurrently.
/// Before the stop-once guard in <see cref="FlowSuppressingHostedService"/>, each hosted
/// service's <c>StopAsync</c> ran twice in parallel (observed here as 4–8 failures per
/// 50 tests), which is how Rebus' non-thread-safe bus dispose produced the reporter's
/// <see cref="ObjectDisposedException"/>.
/// </para>
/// <para>
/// The probe throws — with both stack traces — if it ever observes a second
/// <c>StopAsync</c> entry, so a regression surfaces as loud test failures.
/// </para>
/// </summary>
public class HostStopExactlyOnceProbeTests : WebApplicationTest<TestWebAppFactory, Program>
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IHostedService, StopProbeHostedService>();
    }

    [Test]
    [Repeat(50)]
    public async Task Host_Hosted_Service_Stops_Exactly_Once_Under_Parallel_Churn()
    {
        var client = Factory.CreateClient();
        using var response = await client.GetAsync("/");

        // Small jitter so test completions (and After-hook disposals) overlap heavily.
        await Task.Delay(Random.Shared.Next(0, 20));
    }
}

/// <summary>
/// Throws on a second StopAsync entry — sequential re-stop or concurrent overlap — so the
/// offending call site's stack appears in the test output alongside the first stop's stack.
/// </summary>
internal sealed class StopProbeHostedService : IHostedService
{
    private int _stopEntries;
    private volatile string? _firstStopStack;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var entry = Interlocked.Increment(ref _stopEntries);

        if (entry > 1)
        {
            throw new InvalidOperationException(
                $"StopAsync entered {entry} times on probe {GetHashCode():x8}.\n" +
                $"--- first stop stack ---\n{_firstStopStack}\n" +
                $"--- this stop stack ---\n{Environment.StackTrace}");
        }

        _firstStopStack = Environment.StackTrace;

        // Widen the window like a real bus shutdown (Rebus stops workers, waits on its
        // cleanup task for up to 5s). A concurrent second stop lands inside this delay.
        await Task.Delay(100, CancellationToken.None);
    }
}
