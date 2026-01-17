using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

/// <summary>
/// A log sink that streams test output in real-time to IDE test explorers.
/// Reads from the test context's OutputWriter (populated by TestOutputSink)
/// and sends only the new content since the last update.
/// </summary>
internal class RealTimeOutputSink : ILogSink
{
    private readonly TUnitMessageBus _messageBus;
    private readonly ConcurrentDictionary<string, int> _lastSentPositions = new();

    public RealTimeOutputSink(TUnitMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public bool IsEnabled(LogLevel level) => true;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        _ = LogAsync(level, message, exception, context);
    }

    public async ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        // Only stream output for test contexts
        if (context is not TestContext testContext)
        {
            return;
        }

        // Read from OutputWriter (the single source of truth)
        // TestOutputSink is registered first, so it has already written the content
        var fullOutput = testContext.GetStandardOutput();
        if (string.IsNullOrEmpty(fullOutput))
        {
            return;
        }

        // Get the last position we sent from, default to 0
        var testId = testContext.TestDetails.TestId;
        var lastPosition = _lastSentPositions.GetOrAdd(testId, 0);

        // If there's new content, send only the new part
        if (fullOutput.Length > lastPosition)
        {
            var newContent = fullOutput.Substring(lastPosition);
            _lastSentPositions[testId] = fullOutput.Length;

            await _messageBus.OutputUpdate(testContext, newContent).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Clears tracking for a completed test to free memory.
    /// </summary>
    internal void ClearTracking(string testId)
    {
        _lastSentPositions.TryRemove(testId, out _);
    }
}
