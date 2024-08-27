using System.Text;
using EnumerableAsyncProcessor.Extensions;

namespace TUnit.Engine.Logging;

internal class DualTextWriter : TextWriter
{
    private readonly List<TextWriter> _textWriters;

    public DualTextWriter(params TextWriter?[] textWriters)
    {
        _textWriters = textWriters.OfType<TextWriter>().ToList();
    }

    public override void Flush()
    {
        _textWriters.ForEach(x => x.Flush());
    }

    public override async Task FlushAsync()
    {
        await _textWriters.ForEachAsync(x => x.FlushAsync()).ProcessInParallel();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _textWriters.ForEachAsync(x => x.FlushAsync(cancellationToken)).ProcessInParallel();
    }

    public override void Write(bool value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override void Write(char value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override void Write(char[]? buffer)
    {
        _textWriters.ForEach(x => x.Write(buffer));
    }

    public override void Write(char[] buffer, int index, int count)
    {
        _textWriters.ForEach(x => x.Write(buffer, index, count));
    }

    public override void Write(decimal value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override void Write(double value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override void Write(int value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override void Write(long value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override void Write(object? value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        foreach (var textWriter in _textWriters)
        {
            textWriter.Write(buffer);
        }
    }

    public override void Write(float value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override void Write(string? value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override void Write(string format, object? arg0)
    {
        _textWriters.ForEach(x => x.Write(format, arg0));
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        _textWriters.ForEach(x => x.Write(format, arg0, arg1));
    }

    public override void Write(string format, object? arg0, object? arg1, object? arg2)
    {
        _textWriters.ForEach(x => x.Write(format, arg0, arg1, arg2));
    }

    public override void Write(string format, params object?[] arg)
    {
        _textWriters.ForEach(x => x.Write(format, arg));
    }

    public override void Write(StringBuilder? value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override void Write(uint value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override void Write(ulong value)
    {
        _textWriters.ForEach(x => x.Write(value));
    }

    public override async Task WriteAsync(char value)
    {
        await _textWriters.ForEachAsync(x => x.WriteAsync(value)).ProcessInParallel();
    }

    public override async Task WriteAsync(char[] buffer, int index, int count)
    {
        await _textWriters.ForEachAsync(x => x.WriteAsync(buffer, index, count)).ProcessInParallel();
    }

    public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
    {
        await _textWriters.ForEachAsync(x => x.WriteAsync(buffer, cancellationToken)).ProcessInParallel();
    }

    public override async Task WriteAsync(string? value)
    {
        await _textWriters.ForEachAsync(x => x.WriteAsync(value)).ProcessInParallel();
    }

    public override async Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new())
    {
        await _textWriters.ForEachAsync(x => x.WriteAsync(value, cancellationToken)).ProcessInParallel();
    }

    public override void WriteLine()
    {
        _textWriters.ForEach(x => x.WriteLine());
    }

    public override void WriteLine(bool value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override void WriteLine(char value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override void WriteLine(char[]? buffer)
    {
        _textWriters.ForEach(x => x.WriteLine(buffer));
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        _textWriters.ForEach(x => x.WriteLine(buffer, index, count));
    }

    public override void WriteLine(decimal value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override void WriteLine(double value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override void WriteLine(int value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override void WriteLine(long value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override void WriteLine(object? value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        foreach (var textWriter in _textWriters)
        {
            textWriter.WriteLine(buffer);
        }
    }

    public override void WriteLine(float value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override void WriteLine(string? value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override void WriteLine(string format, object? arg0)
    {
        _textWriters.ForEach(x => x.WriteLine(format, arg0));
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        _textWriters.ForEach(x => x.WriteLine(format, arg0, arg1));
    }

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
    {
        _textWriters.ForEach(x => x.WriteLine(format, arg0, arg1, arg2));
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        _textWriters.ForEach(x => x.WriteLine(format, arg));
    }

    public override void WriteLine(StringBuilder? value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override void WriteLine(uint value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override void WriteLine(ulong value)
    {
        _textWriters.ForEach(x => x.WriteLine(value));
    }

    public override async Task WriteLineAsync()
    {
        await _textWriters.ForEachAsync(x => x.WriteLineAsync()).ProcessInParallel();
    }

    public override async Task WriteLineAsync(char value)
    {
        await _textWriters.ForEachAsync(x => x.WriteLineAsync(value)).ProcessInParallel();
    }

    public override async Task WriteLineAsync(char[] buffer, int index, int count)
    {
        await _textWriters.ForEachAsync(x => x.WriteLineAsync(buffer, index, count)).ProcessInParallel();
    }

    public override async Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
    {
        await _textWriters.ForEachAsync(x => x.WriteLineAsync(buffer, cancellationToken)).ProcessInParallel();
    }

    public override async Task WriteLineAsync(string? value)
    {
        await _textWriters.ForEachAsync(x => x.WriteLineAsync(value)).ProcessInParallel();
    }

    public override async Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new())
    {
        await _textWriters.ForEachAsync(x => x.WriteLineAsync(value, cancellationToken)).ProcessInParallel();
    }

    public override Encoding Encoding => _textWriters.First().Encoding;

    public override string NewLine
    {
        get => _textWriters.First().NewLine;
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        set
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        {
            foreach (var textWriter in _textWriters)
            {
                textWriter.NewLine = value;
            }
        }
    }

    public override void Close()
    {
        Flush();
    }

    protected override void Dispose(bool disposing)
    {
        Flush();
    }

    public override async ValueTask DisposeAsync()
    {
        await FlushAsync();
    }
}