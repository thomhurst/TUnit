using System.Text;
using TUnit.Core;
using TUnit.Core.Logging;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace TUnit.Engine.Logging;

/// <summary>
/// Console interceptor that captures output and routes it to registered sinks.
/// The interceptor itself doesn't write anywhere - it only routes to sinks:
/// - TestOutputSink: accumulates to Context.OutputWriter/ErrorOutputWriter
/// - ConsoleOutputSink: writes to actual console
/// - RealTimeOutputSink: streams to IDEs
/// </summary>
internal abstract class OptimizedConsoleInterceptor : TextWriter
{
    private readonly StringBuilder _lineBuffer = new();

    public override Encoding Encoding => Encoding.UTF8;

    /// <summary>
    /// Gets the log level to use when routing console output to sinks.
    /// </summary>
    protected abstract LogLevel SinkLogLevel { get; }

    private protected abstract TextWriter GetOriginalOut();

    private protected abstract void ResetDefault();

    /// <summary>
    /// Routes the message to registered log sinks.
    /// </summary>
    private void RouteToSinks(string? message)
    {
        if (message is not null && message.Length > 0)
        {
            LogSinkRouter.RouteToSinks(SinkLogLevel, message, null, Context.Current);
        }
    }

    /// <summary>
    /// Routes the message to registered log sinks asynchronously.
    /// </summary>
    private async ValueTask RouteToSinksAsync(string? message)
    {
        if (message is not null && message.Length > 0)
        {
            await LogSinkRouter.RouteToSinksAsync(SinkLogLevel, message, null, Context.Current).ConfigureAwait(false);
        }
    }

#if NET
    public override ValueTask DisposeAsync()
    {
        ResetDefault();
        return ValueTask.CompletedTask;
    }
#endif

    public override void Flush()
    {
        // Flush any buffered partial line
        if (_lineBuffer.Length > 0)
        {
            RouteToSinks(_lineBuffer.ToString());
            _lineBuffer.Clear();
        }
    }

    public override async Task FlushAsync()
    {
        if (_lineBuffer.Length > 0)
        {
            await RouteToSinksAsync(_lineBuffer.ToString()).ConfigureAwait(false);
            _lineBuffer.Clear();
        }
    }

    // Write methods - buffer partial writes until we get a complete line
    public override void Write(bool value) => Write(value.ToString());
    public override void Write(char value) => BufferChar(value);
    public override void Write(char[]? buffer)
    {
        if (buffer != null)
        {
            BufferChars(buffer, 0, buffer.Length);
        }
    }
    public override void Write(decimal value) => Write(value.ToString());
    public override void Write(double value) => Write(value.ToString());
    public override void Write(int value) => Write(value.ToString());
    public override void Write(long value) => Write(value.ToString());
    public override void Write(object? value) => Write(value?.ToString() ?? string.Empty);
    public override void Write(float value) => Write(value.ToString());
    public override void Write(string? value)
    {
        if (value == null) return;
        _lineBuffer.Append(value);
    }
    public override void Write(uint value) => Write(value.ToString());
    public override void Write(ulong value) => Write(value.ToString());
    public override void Write(char[] buffer, int index, int count) => BufferChars(buffer, index, count);
    public override void Write(string format, object? arg0) => Write(string.Format(format, arg0));
    public override void Write(string format, object? arg0, object? arg1) => Write(string.Format(format, arg0, arg1));
    public override void Write(string format, object? arg0, object? arg1, object? arg2) => Write(string.Format(format, arg0, arg1, arg2));
    public override void Write(string format, params object?[] arg) => Write(string.Format(format, arg));

    private void BufferChar(char value)
    {
        _lineBuffer.Append(value);
    }

    private void BufferChars(char[] buffer, int index, int count)
    {
        _lineBuffer.Append(buffer, index, count);
    }

    // WriteLine methods - flush buffer and route complete line to sinks
    public override void WriteLine()
    {
        var line = _lineBuffer.ToString();
        _lineBuffer.Clear();
        RouteToSinks(line);
    }

    public override void WriteLine(bool value) => WriteLine(value.ToString());
    public override void WriteLine(char value) => WriteLine(value.ToString());
    public override void WriteLine(char[]? buffer) => WriteLine(buffer != null ? new string(buffer) : string.Empty);
    public override void WriteLine(char[] buffer, int index, int count) => WriteLine(new string(buffer, index, count));
    public override void WriteLine(decimal value) => WriteLine(value.ToString());
    public override void WriteLine(double value) => WriteLine(value.ToString());
    public override void WriteLine(int value) => WriteLine(value.ToString());
    public override void WriteLine(long value) => WriteLine(value.ToString());
    public override void WriteLine(object? value) => WriteLine(value?.ToString() ?? string.Empty);
    public override void WriteLine(float value) => WriteLine(value.ToString());

    public override void WriteLine(string? value)
    {
        // Prepend any buffered content
        if (_lineBuffer.Length > 0)
        {
            _lineBuffer.Append(value);
            value = _lineBuffer.ToString();
            _lineBuffer.Clear();
        }
        RouteToSinks(value);
    }

    public override void WriteLine(uint value) => WriteLine(value.ToString());
    public override void WriteLine(ulong value) => WriteLine(value.ToString());
    public override void WriteLine(string format, object? arg0) => WriteLine(string.Format(format, arg0));
    public override void WriteLine(string format, object? arg0, object? arg1) => WriteLine(string.Format(format, arg0, arg1));
    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2) => WriteLine(string.Format(format, arg0, arg1, arg2));
    public override void WriteLine(string format, params object?[] arg) => WriteLine(string.Format(format, arg));

    // Async methods
    public override Task WriteLineAsync() => WriteLineAsync(string.Empty);
    public override Task WriteAsync(char value) { Write(value); return Task.CompletedTask; }
    public override Task WriteAsync(char[] buffer, int index, int count) { Write(buffer, index, count); return Task.CompletedTask; }
    public override Task WriteAsync(string? value) { Write(value); return Task.CompletedTask; }

    public override async Task WriteLineAsync(char value)
    {
        await WriteLineAsync(value.ToString()).ConfigureAwait(false);
    }

    public override async Task WriteLineAsync(char[] buffer, int index, int count)
    {
        await WriteLineAsync(new string(buffer, index, count)).ConfigureAwait(false);
    }

    public override async Task WriteLineAsync(string? value)
    {
        if (_lineBuffer.Length > 0)
        {
            _lineBuffer.Append(value);
            value = _lineBuffer.ToString();
            _lineBuffer.Clear();
        }
        await RouteToSinksAsync(value).ConfigureAwait(false);
    }

#if NET
    public override void Write(ReadOnlySpan<char> buffer) => Write(new string(buffer));
    public override void Write(StringBuilder? value) => Write(value?.ToString() ?? string.Empty);
    public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
        => WriteAsync(new string(buffer.Span));
    public override Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new())
        => WriteAsync(value?.ToString() ?? string.Empty);
    public override void WriteLine(ReadOnlySpan<char> buffer) => WriteLine(new string(buffer));
    public override void WriteLine(StringBuilder? value) => WriteLine(value?.ToString() ?? string.Empty);
    public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
        => WriteLineAsync(new string(buffer.Span));
    public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new())
        => WriteLineAsync(value?.ToString() ?? string.Empty);
#endif

    public override IFormatProvider FormatProvider => GetOriginalOut().FormatProvider;

    public override string NewLine
    {
        get => GetOriginalOut().NewLine;
        set => GetOriginalOut().NewLine = value;
    }

    public override void Close()
    {
        Flush();
        ResetDefault();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Flush();
        }
        base.Dispose(disposing);
    }
}
