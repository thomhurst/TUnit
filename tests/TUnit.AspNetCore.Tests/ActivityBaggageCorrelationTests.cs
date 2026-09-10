using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TUnit.Core;
using TUnit.Logging.Microsoft;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Tests that Activity baggage propagation is sufficient for correlated logging,
/// validating the <c>TestContext.ResolveFromActivityBaggage()</c> fallback path.
/// </summary>
public class ActivityBaggageCorrelationTests
{
    /// <summary>
    /// Verifies that <see cref="TestContext.Current"/> resolves via Activity baggage
    /// on a thread where AsyncLocal is null, and that Console output is attributed
    /// to the correct test.
    /// </summary>
    [Test]
    public async Task ActivityBaggage_ResolvesTestContext_WhenAsyncLocalIsNull()
    {
        var testContext = TestContext.Current!;
        var marker = Guid.NewGuid().ToString("N");

        await RunWithSuppressedFlow(() =>
        {
            using var activity = new Activity("test-server-request")
                .SetBaggage(TUnitActivitySource.TagTestId, testContext.Id)
                .Start();

            Console.WriteLine($"ACTIVITY_RESOLVED:{marker}");
        });

        var output = testContext.GetStandardOutput();
        await Assert.That(output).Contains($"ACTIVITY_RESOLVED:{marker}");
    }

    /// <summary>
    /// Verifies that <see cref="CorrelatedTUnitLogger"/> writes to the correct test output
    /// when the only correlation mechanism is Activity baggage.
    /// </summary>
    [Test]
    public async Task CorrelatedLogger_WritesToCorrectTest_ViaActivityBaggage()
    {
        var testContext = TestContext.Current!;
        var marker = Guid.NewGuid().ToString("N");

        await RunWithSuppressedFlow(() =>
        {
            using var activity = new Activity("test-server-request")
                .SetBaggage(TUnitActivitySource.TagTestId, testContext.Id)
                .Start();

            using var provider = new CorrelatedTUnitLoggerProvider();
            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("CORRELATED_BAGGAGE:{Marker}", marker);
        });

        var output = testContext.GetStandardOutput();
        await Assert.That(output).Contains($"CORRELATED_BAGGAGE:{marker}");
    }

    /// <summary>
    /// Verifies that nested Activities don't break baggage resolution —
    /// <see cref="Activity.GetBaggageItem"/> traverses the parent chain.
    /// </summary>
    [Test]
    public async Task NestedActivities_BaggageTraversesParentChain()
    {
        var testContext = TestContext.Current!;
        var marker = Guid.NewGuid().ToString("N");

        await RunWithSuppressedFlow(() =>
        {
            // Parent Activity with baggage
            using var parent = new Activity("parent-request")
                .SetBaggage(TUnitActivitySource.TagTestId, testContext.Id)
                .Start();

            // Child Activity (e.g., middleware creating its own span) — no baggage set directly
            using var child = new Activity("child-middleware").Start();

            // Activity.Current is now the child, but GetBaggageItem traverses parents
            using var provider = new CorrelatedTUnitLoggerProvider();
            var logger = provider.CreateLogger("NestedTest");
            logger.LogInformation("NESTED_ACTIVITY:{Marker}", marker);
        });

        var output = testContext.GetStandardOutput();
        await Assert.That(output).Contains($"NESTED_ACTIVITY:{marker}");
    }

    /// <summary>
    /// Verifies that <see cref="CorrelatedTUnitLogger"/> produces no output when
    /// neither AsyncLocal nor Activity baggage provides a test context.
    /// </summary>
    [Test]
    public async Task CorrelatedLogger_IsNoOp_WhenNoContextAvailable()
    {
        var testContext = TestContext.Current!;
        var marker = Guid.NewGuid().ToString("N");

        await RunWithSuppressedFlow(() =>
        {
            // No AsyncLocal, no Activity — TestContext.Current should be null
            using var provider = new CorrelatedTUnitLoggerProvider();
            var logger = provider.CreateLogger("NoContext");
            logger.LogInformation("SHOULD_NOT_APPEAR:{Marker}", marker);
        });

        var output = testContext.GetStandardOutput();
        await Assert.That(output).DoesNotContain($"SHOULD_NOT_APPEAR:{marker}");
    }

    /// <summary>
    /// Verifies that <see cref="CorrelatedTUnitLogger"/> skips output when a per-test
    /// <see cref="TUnitLoggerProvider"/> is active for the same test context (duplicate suppression).
    /// </summary>
    [Test]
    public async Task CorrelatedLogger_SkipsOutput_WhenPerTestLoggerActive()
    {
        var testContext = TestContext.Current!;
        var marker = Guid.NewGuid().ToString("N");

        // Register a per-test logger provider to activate duplicate suppression
        using var perTestProvider = new TUnitLoggerProvider(testContext);

        using var correlatedProvider = new CorrelatedTUnitLoggerProvider();
        var logger = correlatedProvider.CreateLogger("DuplicateTest");
        logger.LogInformation("SHOULD_BE_SUPPRESSED:{Marker}", marker);

        var output = testContext.GetStandardOutput();
        await Assert.That(output).DoesNotContain($"SHOULD_BE_SUPPRESSED:{marker}");
    }

    /// <summary>
    /// Verifies that <see cref="CorrelatedTUnitLogger"/> respects the minimum log level.
    /// </summary>
    [Test]
    public async Task CorrelatedLogger_RespectsMinLogLevel()
    {
        var testContext = TestContext.Current!;
        var marker = Guid.NewGuid().ToString("N");

        using var provider = new CorrelatedTUnitLoggerProvider(LogLevel.Warning);
        var logger = provider.CreateLogger("LogLevelTest");

        // Info is below Warning — should not appear
        logger.LogInformation("BELOW_LEVEL:{Marker}", marker);

        // Warning meets the threshold — should appear
        logger.LogWarning("AT_LEVEL:{Marker}", marker);

        var output = testContext.GetStandardOutput();
        await Assert.That(output).DoesNotContain($"BELOW_LEVEL:{marker}");
        await Assert.That(output).Contains($"AT_LEVEL:{marker}");
    }

    /// <summary>
    /// Verifies that Error+ log messages are routed to <see cref="Console.Error"/>.
    /// </summary>
    [Test]
    public async Task CorrelatedLogger_RoutesErrorToStdErr()
    {
        var testContext = TestContext.Current!;
        var marker = Guid.NewGuid().ToString("N");

        using var provider = new CorrelatedTUnitLoggerProvider();
        var logger = provider.CreateLogger("ErrorRouting");

        logger.LogError("ERROR_MSG:{Marker}", marker);

        var errorOutput = testContext.GetErrorOutput();
        await Assert.That(errorOutput).Contains($"ERROR_MSG:{marker}");
    }

    /// <summary>
    /// Runs an action on a thread pool thread with suppressed execution context flow.
    /// The worker thread will have no AsyncLocal values from the test thread.
    /// </summary>
    private static async Task RunWithSuppressedFlow(Action action)
    {
        // SuppressFlow + Undo must run on the same thread.
        // Capture the task without awaiting, Undo immediately, then await.
        var flowControl = ExecutionContext.SuppressFlow();
        Task task;
        try
        {
            task = Task.Run(action);
        }
        finally
        {
            flowControl.Undo();
        }

        await task;
    }
}
