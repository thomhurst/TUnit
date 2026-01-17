using TUnit.Core;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

/// <summary>
/// A log sink that streams test output to IDEs in real-time by sending
/// TestNodeUpdateMessage updates via the message bus.
/// </summary>
internal class IdeOutputLogSink : ILogSink
{
    private readonly TUnitMessageBus _messageBus;

    public IdeOutputLogSink(TUnitMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public bool IsEnabled(LogLevel level) => level >= LogLevel.Information;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        // Fire and forget - the async call will complete in background
        _ = LogAsync(level, message, exception, context);
    }

    public async ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        // Only stream output for test contexts (not assembly/class hooks etc.)
        if (context is not TestContext testContext)
        {
            return;
        }

        // Send an output update to the IDE
        await _messageBus.OutputUpdate(testContext).ConfigureAwait(false);
    }
}
