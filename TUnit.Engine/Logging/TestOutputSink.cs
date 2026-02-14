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

        // During non-test phases (e.g. data source initialization), write directly to the
        // real console since the output isn't associated with any specific test.
        // Use StandardOut/ErrorConsoleInterceptor.DefaultOut/DefaultError (backed by
        // Console.OpenStandardOutput/Error) to avoid recursion through the interceptor.
        if (context is GlobalContext)
        {
            var consoleWriter = level >= LogLevel.Error
                ? StandardErrorConsoleInterceptor.DefaultError
                : StandardOutConsoleInterceptor.DefaultOut;
            consoleWriter.WriteLine(message);
            return;
        }

        var writer = level >= LogLevel.Error ? context.ErrorOutputWriter : context.OutputWriter;
        writer.WriteLine(message);
    }

    public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (context == null)
        {
            return ValueTask.CompletedTask;
        }

        // During non-test phases (e.g. data source initialization), write directly to the
        // real console since the output isn't associated with any specific test.
        if (context is GlobalContext)
        {
            var consoleWriter = level >= LogLevel.Error
                ? StandardErrorConsoleInterceptor.DefaultError
                : StandardOutConsoleInterceptor.DefaultOut;
            consoleWriter.WriteLine(message);
            return ValueTask.CompletedTask;
        }

        var writer = level >= LogLevel.Error ? context.ErrorOutputWriter : context.OutputWriter;
        writer.WriteLine(message);
        return ValueTask.CompletedTask;
    }
}
