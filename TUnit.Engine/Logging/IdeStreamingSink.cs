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
/// <b>Cumulative Streaming with Heartbeat:</b> Sends full output each update, followed by a
/// heartbeat (no output). Rider concatenates the previous update with the current update, so
/// the heartbeat clears the "previous" to prevent duplication on the next content update.
/// </para>
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

            // Passive cleanup: if test completed, mark as completed and cleanup
            // The atomic flag ensures we never send updates after detecting completion
            if (state.TestContext.Result is not null)
            {
                state.TryMarkCompleted();
                CleanupTest(testId, state);
                return;
            }

            // Double-check: if already marked completed by another path, don't proceed
            if (state.IsCompleted)
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
            // Rider concatenates the previous update with the current update.
            // To prevent duplication, we send a heartbeat (no output) after each content update,
            // so the next content update concatenates with empty = just the current content.
            var output = state.TestContext.GetStandardOutput();
            var error = state.TestContext.GetErrorOutput();

            if (string.IsNullOrEmpty(output) && string.IsNullOrEmpty(error))
            {
                return;
            }

            _ = SendOutputUpdateWithFollowUpHeartbeatAsync(state, output, error);
        }
        catch
        {
            // Swallow exceptions to prevent crashing thread pool
        }
    }

    private async Task SendOutputUpdateWithFollowUpHeartbeatAsync(TestStreamingState state, string? output, string? error)
    {
        try
        {
            var testContext = state.TestContext;

            // Don't send if test already completed - final state has been sent
            if (state.IsCompleted || testContext.Result is not null)
            {
                state.TryMarkCompleted();
                return;
            }

            var testNode = CreateOutputUpdateNode(testContext, output, error);
            if (testNode is null)
            {
                return;
            }

            // Send the content update
            await _messageBus.PublishOutputUpdate(testNode).ConfigureAwait(false);

            // Send a follow-up heartbeat (no output) to clear the "previous update"
            // This prevents Rider from concatenating this content with the next content update
            // CRITICAL: Check again that test hasn't completed - we must never send
            // InProgressTestNodeStateProperty after the final state has been sent
            if (state.IsCompleted || testContext.Result is not null)
            {
                state.TryMarkCompleted();
                return;
            }

            var heartbeat = CreateHeartbeatNode(testContext);
            if (heartbeat is not null)
            {
                await _messageBus.PublishOutputUpdate(heartbeat).ConfigureAwait(false);
            }
        }
        catch
        {
            // Swallow exceptions to prevent disrupting test execution
        }
    }

    private static TestNode? CreateHeartbeatNode(TestContext testContext)
    {
        if (testContext.TestDetails?.TestId is not { } testId)
        {
            return null;
        }

        return new TestNode
        {
            Uid = new TestNodeUid(testId),
            DisplayName = testContext.GetDisplayName(),
            Properties = new PropertyBag(InProgressTestNodeStateProperty.CachedInstance)
        };
    }

    private async Task SendOutputUpdateAsync(TestContext testContext, string? output, string? error)
    {
        try
        {
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

    private static TestNode? CreateOutputUpdateNode(TestContext testContext, string? outputDelta, string? errorDelta)
    {
        // Defensive: ensure TestDetails is available
        if (testContext.TestDetails?.TestId is not { } testId)
        {
            return null;
        }

        // Build properties list with cumulative output
        // Rider replaces the displayed output with each update, so we send full snapshots.
        var properties = new List<IProperty>(3)
        {
            InProgressTestNodeStateProperty.CachedInstance
        };

        if (!string.IsNullOrEmpty(outputDelta))
        {
            properties.Add(new StandardOutputProperty(outputDelta!));
        }

        if (!string.IsNullOrEmpty(errorDelta))
        {
            properties.Add(new StandardErrorProperty(errorDelta!));
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
        private int _isCompleted; // Set to 1 once we detect test completion - never send after this
        private int _lastOutputPosition;
        private int _lastErrorPosition;

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

        /// <summary>
        /// Atomically marks this test as completed. Once marked, no more updates will be sent.
        /// </summary>
        /// <returns>True if this call marked completion (first caller), false if already marked.</returns>
        public bool TryMarkCompleted()
        {
            return Interlocked.Exchange(ref _isCompleted, 1) == 0;
        }

        /// <summary>
        /// Returns true if this test has been marked as completed.
        /// </summary>
        public bool IsCompleted => Interlocked.CompareExchange(ref _isCompleted, 0, 0) == 1;

        /// <summary>
        /// Gets only the new output since the last call (delta).
        /// IDEs like Rider append each update, so sending deltas builds up the correct output.
        /// </summary>
        public (string? Output, string? Error) GetOutputDelta()
        {
            var fullOutput = TestContext.GetStandardOutput();
            var fullError = TestContext.GetErrorOutput();

            string? outputDelta = null;
            string? errorDelta = null;

            if (!string.IsNullOrEmpty(fullOutput) && fullOutput.Length > _lastOutputPosition)
            {
                outputDelta = fullOutput.Substring(_lastOutputPosition);
                _lastOutputPosition = fullOutput.Length;
            }

            if (!string.IsNullOrEmpty(fullError) && fullError.Length > _lastErrorPosition)
            {
                errorDelta = fullError.Substring(_lastErrorPosition);
                _lastErrorPosition = fullError.Length;
            }

            return (outputDelta, errorDelta);
        }

        public void Dispose()
        {
            // Stop timer before disposing to prevent callback race
            Timer?.Change(Timeout.Infinite, Timeout.Infinite);
            Timer?.Dispose();
        }
    }
}
