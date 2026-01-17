using TUnit.Core;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

/// <summary>
/// A log sink that streams test output in real-time by sending
/// TestNodeUpdateMessage updates via the message bus.
/// Enabled for IDE clients and when --output Detailed is used.
/// </summary>
internal class RealTimeOutputSink : ILogSink
{
    private readonly TUnitMessageBus _messageBus;

    public RealTimeOutputSink(TUnitMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public bool IsEnabled(LogLevel level) => level >= LogLevel.Information;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        _ = LogAsync(level, message, exception, context);
    }

    public async ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (!IsEnabled(level) || string.IsNullOrEmpty(message))
        {
            return;
        }

        // Only stream output for test contexts (not assembly/class hooks etc.)
        if (context is not TestContext testContext)
        {
            return;
        }

        // Send the output in real-time via TestNodeUpdateMessage
        await _messageBus.OutputUpdate(testContext, message).ConfigureAwait(false);
    }
}
