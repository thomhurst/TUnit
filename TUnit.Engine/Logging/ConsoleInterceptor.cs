using System.Text;
using TUnit.Engine.Services;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace TUnit.Engine.Logging;

internal abstract class ConsoleInterceptor(VerbosityService verbosityService) : TextWriter
{
    public override Encoding Encoding => RedirectedOut?.Encoding ?? Encoding.UTF8;

    protected abstract TextWriter? RedirectedOut { get; }

    private protected abstract TextWriter GetOriginalOut();

    private protected abstract void ResetDefault();

#if NET
    public override ValueTask DisposeAsync()
    {
        ResetDefault();
        return ValueTask.CompletedTask;
    }
#endif

    public override void Flush()
    {
        GetOriginalOut().Flush();
    }

    private void WriteCore<T>(T value, Action<TextWriter, T> writeAction)
    {
        if (!verbosityService.HideTestOutput)
        {
            writeAction(GetOriginalOut(), value);
        }

        if (RedirectedOut != null)
        {
            writeAction(RedirectedOut, value);
        }
    }

    private async Task WriteAsyncCore<T>(T value, Func<TextWriter, T, Task> writeAction)
    {
        if (!verbosityService.HideTestOutput)
        {
            await writeAction(GetOriginalOut(), value);
        }

        if (RedirectedOut != null)
        {
            await writeAction(RedirectedOut, value);
        }
    }

    public override void Write(bool value) => WriteCore(value, (w, v) => w.Write(v));

    public override void Write(char[]? buffer) => WriteCore(buffer, (w, v) => w.Write(v));

    public override void Write(decimal value) => WriteCore(value, (w, v) => w.Write(v));

    public override void Write(double value) => WriteCore(value, (w, v) => w.Write(v));

    public override void Write(int value) => WriteCore(value, (w, v) => w.Write(v));

    public override void Write(long value) => WriteCore(value, (w, v) => w.Write(v));

    public override void Write(object? value) => WriteCore(value, (w, v) => w.Write(v));

    public override void Write(float value) => WriteCore(value, (w, v) => w.Write(v));

    public override void Write(string format, object? arg0) => WriteCore((format, arg0), (w, v) => w.Write(v.format, v.arg0));

    public override void Write(string format, object? arg0, object? arg1) => WriteCore((format, arg0, arg1), (w, v) => w.Write(v.format, v.arg0, v.arg1));

    public override void Write(string format, object? arg0, object? arg1, object? arg2) => WriteCore((format, arg0, arg1, arg2), (w, v) => w.Write(v.format, v.arg0, v.arg1, v.arg2));

    public override void Write(string format, params object?[] arg) => WriteCore((format, arg), (w, v) => w.Write(v.format, v.arg));

    public override void Write(uint value) => WriteCore(value, (w, v) => w.Write(v));

    public override void Write(ulong value) => WriteCore(value, (w, v) => w.Write(v));

    public override void WriteLine()
    {
        if (!verbosityService.HideTestOutput)
        {
            GetOriginalOut().WriteLine();
        }

        RedirectedOut?.WriteLine();
    }

    public override void WriteLine(bool value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override void WriteLine(char value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override void WriteLine(char[]? buffer) => WriteCore(buffer, (w, v) => w.WriteLine(v));

    public override void WriteLine(char[] buffer, int index, int count) => WriteCore((buffer, index, count), (w, v) => w.WriteLine(v.buffer, v.index, v.count));

    public override void WriteLine(decimal value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override void WriteLine(double value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override void WriteLine(int value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override void WriteLine(long value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override void WriteLine(object? value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override void WriteLine(float value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override void WriteLine(string? value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override void WriteLine(string format, object? arg0) => WriteCore((format, arg0), (w, v) => w.WriteLine(v.format, v.arg0));

    public override void WriteLine(string format, object? arg0, object? arg1) => WriteCore((format, arg0, arg1), (w, v) => w.WriteLine(v.format, v.arg0, v.arg1));

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2) => WriteCore((format, arg0, arg1, arg2), (w, v) => w.WriteLine(v.format, v.arg0, v.arg1, v.arg2));

    public override void WriteLine(string format, params object?[] arg) => WriteCore((format, arg), (w, v) => w.WriteLine(v.format, v.arg));

    public override void WriteLine(uint value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override void WriteLine(ulong value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override Task WriteLineAsync() => WriteAsyncCore(0, async (w, _) => await w.WriteLineAsync());

    public override IFormatProvider FormatProvider => GetOriginalOut().FormatProvider;

    public override string NewLine
    {
        get => GetOriginalOut().NewLine;
        set
        {
            GetOriginalOut().NewLine = value;

            if (RedirectedOut != null)
            {
                RedirectedOut.NewLine = value;
            }
        }
    }

    public override void Close()
    {
        ResetDefault();
    }

    public override Task FlushAsync()
    {
        return GetOriginalOut().FlushAsync();
    }

    public override void Write(char value) => WriteCore(value, (w, v) => w.Write(v));

    public override void Write(char[] buffer, int index, int count) => WriteCore((buffer, index, count), (w, v) => w.Write(v.buffer, v.index, v.count));

    public override void Write(string? value) => WriteCore(value, (w, v) => w.Write(v));

    public override Task WriteAsync(char value) => WriteAsyncCore(value, async (w, v) => await w.WriteAsync(v));

    public override Task WriteAsync(char[] buffer, int index, int count) => WriteAsyncCore((buffer, index, count), async (w, v) => await w.WriteAsync(v.buffer, v.index, v.count));

    public override Task WriteAsync(string? value) => WriteAsyncCore(value, async (w, v) => await w.WriteAsync(v));

#if NET
    public override void Write(ReadOnlySpan<char> buffer)
    {
        if (!verbosityService.HideTestOutput)
        {
            GetOriginalOut().Write(buffer);
        }

        RedirectedOut?.Write(buffer);
    }

    public override void Write(StringBuilder? value) => WriteCore(value, (w, v) => w.Write(v));

    public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new()) => WriteAsyncCore((buffer, cancellationToken), async (w, v) => await w.WriteAsync(v.buffer, v.cancellationToken));

    public override Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new()) => WriteAsyncCore((value, cancellationToken), async (w, v) => await w.WriteAsync(v.value, v.cancellationToken));

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        if (!verbosityService.HideTestOutput)
        {
            GetOriginalOut().WriteLine(buffer);
        }

        RedirectedOut?.WriteLine(buffer);
    }

    public override void WriteLine(StringBuilder? value) => WriteCore(value, (w, v) => w.WriteLine(v));

    public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new()) => WriteAsyncCore((buffer, cancellationToken), async (w, v) => await w.WriteLineAsync(v.buffer, v.cancellationToken));

    public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new()) => WriteAsyncCore((value, cancellationToken), async (w, v) => await w.WriteLineAsync(v.value, v.cancellationToken));
#endif

    public override Task WriteLineAsync(char value) => WriteAsyncCore(value, async (w, v) => await w.WriteLineAsync(v));

    public override Task WriteLineAsync(char[] buffer, int index, int count) => WriteAsyncCore((buffer, index, count), async (w, v) => await w.WriteLineAsync(v.buffer, v.index, v.count));

    public override Task WriteLineAsync(string? value) => WriteAsyncCore(value, async (w, v) => await w.WriteLineAsync(v));
}
