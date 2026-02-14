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
        if (context is not TestContext)
        {
            // Only capture output for test contexts (included in test results).
            // For non-test contexts (e.g. data source initialization, hooks, GlobalContext),
            // write directly to the real console via the pre-interception writers
            // to avoid recursion through the console interceptor and to ensure
            // the output is visible rather than captured in an unread buffer.
            if (context is not null)
            {
                var consoleWriter = level >= LogLevel.Error
                    ? StandardErrorConsoleInterceptor.DefaultError
                    : StandardOutConsoleInterceptor.DefaultOut;
                consoleWriter.WriteLine(message);
            }

            return;
        }

        var writer = level >= LogLevel.Error ? context.ErrorOutputWriter : context.OutputWriter;
        writer.WriteLine(message);
    }

    public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (context is not TestContext)
        {
            if (context is not null)
            {
                var consoleWriter = level >= LogLevel.Error
                    ? StandardErrorConsoleInterceptor.DefaultError
                    : StandardOutConsoleInterceptor.DefaultOut;
                consoleWriter.WriteLine(message);
            }

            return ValueTask.CompletedTask;
        }

        var writer = level >= LogLevel.Error ? context.ErrorOutputWriter : context.OutputWriter;
        writer.WriteLine(message);
        return ValueTask.CompletedTask;
    }
}
