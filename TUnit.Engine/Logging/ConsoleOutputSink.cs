using TUnit.Core;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

/// <summary>
/// A log sink that writes output to the actual console (stdout/stderr).
/// Only registered when --output Detailed is specified.
/// </summary>
internal sealed class ConsoleOutputSink : ILogSink
{
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;

    public ConsoleOutputSink(TextWriter stdout, TextWriter stderr)
    {
        _stdout = stdout;
        _stderr = stderr;
    }

    public bool IsEnabled(LogLevel level) => true;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        var writer = level >= LogLevel.Error ? _stderr : _stdout;
        writer.WriteLine(message);
    }

    public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        Log(level, message, exception, context);
        return ValueTask.CompletedTask;
    }
}
