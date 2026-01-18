using System.Collections.Concurrent;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Logging;

#pragma warning disable TPEXP

namespace TUnit.Engine.Logging;

/// <summary>
/// A log sink that streams test output in real-time to IDE test explorers.
/// Sends cumulative output snapshots every 1 second during test execution.
/// Only activated when running in an IDE environment (not console).
/// </summary>
/// <remarks>
/// <para>
/// <b>Cleanup Strategy:</b> Uses passive cleanup - each timer tick checks if the test
/// has completed (Result is not null) and cleans up if so. This avoids the need to
/// register for test completion events while ensuring timely resource release.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Uses Interlocked operations for the dirty flag and
/// ConcurrentDictionary for test state tracking. Timer callbacks are wrapped
/// in try-catch to prevent thread pool crashes.
/// </para>
/// </remarks>
internal sealed class IdeStreamingSink : ILogSink, IAsyncDisposable
{
    private readonly TUnitMessageBus _messageBus;
    private readonly ConcurrentDictionary<string, TestStreamingState> _activeTests = new();
    private readonly TimeSpan _throttleInterval = TimeSpan.FromSeconds(1);

    public IdeStreamingSink(TUnitMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public bool IsEnabled(LogLevel level) => true;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        try
        {
            if (context is not TestContext testContext)
            {
                return;
            }

            // Only stream for tests that have started execution (TestStart is set)
            if (testContext.TestDetails?.TestId is not { } testId ||
                testContext.Execution.TestStart is null)
            {
                return;
            }

            var state = _activeTests.GetOrAdd(testId, _ => CreateStreamingState(testContext));

            state.MarkDirty();
        }
        catch
        {
            // Swallow exceptions to prevent disrupting test execution
        }
    }

    public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        Log(level, message, exception, context);
        return ValueTask.CompletedTask;
    }

    private TestStreamingState CreateStreamingState(TestContext testContext)
    {
        var state = new TestStreamingState(testContext);

        state.Timer = new Timer(
            callback: _ => OnTimerTick(testContext.TestDetails.TestId),
            state: null,
            dueTime: _throttleInterval,
            period: _throttleInterval);

        return state;
    }

    private void OnTimerTick(string testId)
    {
        try
        {
            if (!_activeTests.TryGetValue(testId, out var state))
            {
                return;
            }

            // Passive cleanup: if test completed, dispose and remove
            if (state.TestContext.Result is not null)
            {
                CleanupTest(testId, state);
                return;
            }

            // Skip if no new output since last send
            if (!state.TryConsumeAndReset())
            {
                return;
            }

            // Send cumulative output snapshot
            _ = SendOutputUpdateAsync(state.TestContext);
        }
        catch
        {
            // Swallow exceptions to prevent crashing thread pool
        }
    }

    private async Task SendOutputUpdateAsync(TestContext testContext)
    {
        try
        {
            var output = testContext.GetStandardOutput();
            var error = testContext.GetErrorOutput();

            if (string.IsNullOrEmpty(output) && string.IsNullOrEmpty(error))
            {
                return;
            }

            var testNode = CreateOutputUpdateNode(testContext, output, error);
            if (testNode is null)
            {
                return;
            }

            await _messageBus.PublishOutputUpdate(testNode).ConfigureAwait(false);
        }
        catch
        {
            // Swallow exceptions to prevent disrupting test execution
        }
    }

    private static TestNode? CreateOutputUpdateNode(TestContext testContext, string? output, string? error)
    {
        // Defensive: ensure TestDetails is available
        if (testContext.TestDetails?.TestId is not { } testId)
        {
            return null;
        }

        var properties = new List<IProperty>
        {
            InProgressTestNodeStateProperty.CachedInstance
        };

        if (!string.IsNullOrEmpty(output))
        {
            properties.Add(new StandardOutputProperty(output!));
        }

        if (!string.IsNullOrEmpty(error))
        {
            properties.Add(new StandardErrorProperty(error!));
        }

        return new TestNode
        {
            Uid = new TestNodeUid(testId),
            DisplayName = testContext.GetDisplayName(),
            Properties = new PropertyBag(properties)
        };
    }

    private void CleanupTest(string testId, TestStreamingState state)
    {
        state.Dispose();
        _activeTests.TryRemove(testId, out _);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var kvp in _activeTests)
        {
            kvp.Value.Dispose();
        }

        _activeTests.Clear();

        await ValueTask.CompletedTask;
    }

    private sealed class TestStreamingState : IDisposable
    {
        private int _isDirty;

        public TestContext TestContext { get; }
        public Timer? Timer { get; set; }

        public TestStreamingState(TestContext testContext)
        {
            TestContext = testContext;
        }

        public void MarkDirty()
        {
            Interlocked.Exchange(ref _isDirty, 1);
        }

        public bool TryConsumeAndReset()
        {
            return Interlocked.Exchange(ref _isDirty, 0) == 1;
        }

        public void Dispose()
        {
            // Stop timer before disposing to prevent callback race
            Timer?.Change(Timeout.Infinite, Timeout.Infinite);
            Timer?.Dispose();
        }
    }
}
