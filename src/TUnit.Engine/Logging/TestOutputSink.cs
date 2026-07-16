using TUnit.Core;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

/// <summary>
/// A log sink that accumulates output to the test context's output writers.
/// Routes to OutputWriter for non-error levels and ErrorOutputWriter for error levels.
/// This captured output is included in test results.
/// </summary>
internal sealed class TestOutputSink : ILogSink
{
    public bool IsEnabled(LogLevel level) => true;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (context == null)
        {
            return;
        }

        var writer = level >= LogLevel.Error ? context.ErrorOutputWriter : context.OutputWriter;
        writer.WriteLine(message);
    }

    public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        Log(level, message, exception, context);
        return ValueTask.CompletedTask;
    }
}
