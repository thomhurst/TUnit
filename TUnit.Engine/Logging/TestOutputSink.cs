using TUnit.Core;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

/// <summary>
/// A log sink that accumulates output to the test context's output writers.
/// Routes to OutputWriter for non-error levels and ErrorOutputWriter for error levels.
/// This captured output is included in test results.
/// Only captures output for TestContext — non-test contexts (hooks, data source
/// initialization, GlobalContext) are handled by ConsoleOutputSink when registered.
/// </summary>
internal sealed class TestOutputSink : ILogSink
{
    public bool IsEnabled(LogLevel level) => true;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (context is not TestContext)
        {
            return;
        }

        var writer = level >= LogLevel.Error ? context.ErrorOutputWriter : context.OutputWriter;
        writer.WriteLine(message);
    }

    public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (context is not TestContext)
        {
            return ValueTask.CompletedTask;
        }

        var writer = level >= LogLevel.Error ? context.ErrorOutputWriter : context.OutputWriter;
        writer.WriteLine(message);
        return ValueTask.CompletedTask;
    }
}
