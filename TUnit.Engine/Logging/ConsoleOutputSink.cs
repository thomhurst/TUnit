using TUnit.Core;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

/// <summary>
/// A log sink that writes output to the actual console (stdout/stderr).
/// Always registered. In detailed mode, writes all output. In non-detailed mode,
/// only writes non-test output (hooks, data source initialization, etc.) so that
/// infrastructure output is always visible on the console.
/// </summary>
internal sealed class ConsoleOutputSink : ILogSink
{
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;
    private readonly bool _detailedOutput;

    public ConsoleOutputSink(TextWriter stdout, TextWriter stderr, bool detailedOutput)
    {
        _stdout = stdout;
        _stderr = stderr;
        _detailedOutput = detailedOutput;
    }

    public bool IsEnabled(LogLevel level) => true;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (!_detailedOutput && context is TestContext)
        {
            return;
        }

        var writer = level >= LogLevel.Error ? _stderr : _stdout;
        writer.WriteLine(message);
    }

    public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        Log(level, message, exception, context);
        return ValueTask.CompletedTask;
    }
}
