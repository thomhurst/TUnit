using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TUnit.Core.Logging;

internal class ThreadSafeStringWriter : StringWriter
{
    private readonly Lock _lock = new();
    
    public override void Close()
    {
        lock (_lock)
        {
            base.Close();
        }
    }

    protected override void Dispose(bool disposing)
    {
        lock (_lock)
        {
            base.Dispose(disposing);
        }
    }

    public override Task FlushAsync()
    {
        lock (_lock)
        {
            return base.FlushAsync();
        }
    }

    public override StringBuilder GetStringBuilder()
    {
        lock (_lock)
        {
            return base.GetStringBuilder();
        }
    }

    public override string ToString()
    {
        lock (_lock)
        {
            return base.GetStringBuilder().ToString();
        }
    }

    public override void Write(char value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override void Write(char[] buffer, int index, int count)
    {
        lock (_lock)
        {
            base.Write(buffer, index, count);
        }
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        lock (_lock)
        {
            base.Write(buffer);
        }
    }

    public override void Write(string? value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override void Write(StringBuilder? value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override Task WriteAsync(char value)
    {
        lock (_lock)
        {
            return base.WriteAsync(value);
        }
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        lock (_lock)
        {
            return base.WriteAsync(buffer, index, count);
        }
    }

    public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        lock (_lock)
        {
            return base.WriteAsync(buffer, cancellationToken);
        }
    }

    public override Task WriteAsync(string? value)
    {
        lock (_lock)
        {
            return base.WriteAsync(value);
        }
    }

    public override Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new CancellationToken())
    {
        lock (_lock)
        {
            return base.WriteAsync(value, cancellationToken);
        }
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        lock (_lock)
        {
            base.WriteLine(buffer);
        }
    }

    public override void WriteLine(StringBuilder? value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override Task WriteLineAsync(char value)
    {
        lock (_lock)
        {
            return base.WriteLineAsync(value);
        }
    }

    public override Task WriteLineAsync(char[] buffer, int index, int count)
    {
        lock (_lock)
        {
            return base.WriteLineAsync(buffer, index, count);
        }
    }

    public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        lock (_lock)
        {
            return base.WriteLineAsync(buffer, cancellationToken);
        }
    }

    public override Task WriteLineAsync(string? value)
    {
        lock (_lock)
        {
            return base.WriteLineAsync(value);
        }
    }

    public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new CancellationToken())
    {
        lock (_lock)
        {
            return base.WriteLineAsync(value, cancellationToken);
        }
    }

    public override Encoding Encoding
    {
        get
        {
            lock (_lock)
            {
                return base.Encoding;
            }
        }
    }

    public override ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            return base.DisposeAsync();
        }
    }

    public override void Flush()
    {
        lock (_lock)
        {
            base.Flush();
        }
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return base.FlushAsync(cancellationToken);
        }
    }

    public override void Write(bool value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override void Write(char[]? buffer)
    {
        lock (_lock)
        {
            base.Write(buffer);
        }
    }

    public override void Write(decimal value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override void Write(double value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override void Write(int value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override void Write(long value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override void Write(object? value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override void Write(float value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override void Write(string format, object? arg0)
    {
        lock (_lock)
        {
            base.Write(format, arg0);
        }
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        lock (_lock)
        {
            base.Write(format, arg0, arg1);
        }
    }

    public override void Write(string format, object? arg0, object? arg1, object? arg2)
    {
        lock (_lock)
        {
            base.Write(format, arg0, arg1, arg2);
        }
    }

    public override void Write(string format, params object?[] arg)
    {
        lock (_lock)
        {
            base.Write(format, arg);
        }
    }

    public override void Write(string format, params ReadOnlySpan<object?> arg)
    {
        lock (_lock)
        {
            base.Write(format, arg);
        }
    }

    public override void Write(uint value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override void Write(ulong value)
    {
        lock (_lock)
        {
            base.Write(value);
        }
    }

    public override void WriteLine()
    {
        lock (_lock)
        {
            base.WriteLine();
        }
    }

    public override void WriteLine(bool value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override void WriteLine(char value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override void WriteLine(char[]? buffer)
    {
        lock (_lock)
        {
            base.WriteLine(buffer);
        }
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        lock (_lock)
        {
            base.WriteLine(buffer, index, count);
        }
    }

    public override void WriteLine(decimal value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override void WriteLine(double value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override void WriteLine(int value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override void WriteLine(long value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override void WriteLine(object? value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override void WriteLine(float value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override void WriteLine(string? value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override void WriteLine(string format, object? arg0)
    {
        lock (_lock)
        {
            base.WriteLine(format, arg0);
        }
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        lock (_lock)
        {
            base.WriteLine(format, arg0, arg1);
        }
    }

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
    {
        lock (_lock)
        {
            base.WriteLine(format, arg0, arg1, arg2);
        }
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        lock (_lock)
        {
            base.WriteLine(format, arg);
        }
    }

    public override void WriteLine(string format, params ReadOnlySpan<object?> arg)
    {
        lock (_lock)
        {
            base.WriteLine(format, arg);
        }
    }

    public override void WriteLine(uint value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override void WriteLine(ulong value)
    {
        lock (_lock)
        {
            base.WriteLine(value);
        }
    }

    public override Task WriteLineAsync()
    {
        lock (_lock)
        {
            return base.WriteLineAsync();
        }
    }

    public override IFormatProvider FormatProvider
    {
        get
        {
            lock (_lock)
            {
                return base.FormatProvider;
            }
        }
    }

    [AllowNull] public override string NewLine
    {
        get
        {
            lock (_lock)
            {
                return base.NewLine;
            }
        }
        set
        {
            lock (_lock)
            {
                base.NewLine = value;
            }
        }
    }

    public override object InitializeLifetimeService()
    {
        lock (_lock)
        {
            return base.InitializeLifetimeService();
        }
    }

    public override bool Equals(object? obj)
    {
        lock (_lock)
        {
            return base.Equals(obj);
        }
    }

    public override int GetHashCode()
    {
        lock (_lock)
        {
            return base.GetHashCode();
        }
    }
}