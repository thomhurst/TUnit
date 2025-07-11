using System.Buffers;
using System.Text;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member

namespace TUnit.Engine.Logging;

/// <summary>
/// A thread-safe buffered text writer that reduces allocation overhead
/// </summary>
internal sealed class BufferedTextWriter : TextWriter, IDisposable
{
    private readonly TextWriter _target;
    private readonly object _lock = new();
    private readonly int _bufferSize;
    private readonly StringBuilder _buffer;
    private bool _disposed;

    public BufferedTextWriter(TextWriter target, int bufferSize = 4096)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _bufferSize = bufferSize;
        _buffer = new StringBuilder(bufferSize);
    }

    public override Encoding Encoding => _target.Encoding;

    public override IFormatProvider FormatProvider => _target.FormatProvider;

    public override string NewLine
    {
        get => _target.NewLine;
        set
        {
            lock (_lock)
            {
                _target.NewLine = value ?? Environment.NewLine;
            }
        }
    }

    public override void Write(char value)
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.Append(value);
            CheckFlush();
        }
    }

    public override void Write(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;

        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.Append(value);
            CheckFlush();
        }
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer == null || count <= 0) return;

        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.Append(buffer, index, count);
            CheckFlush();
        }
    }

    public override void WriteLine()
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.AppendLine();
            FlushBuffer();
        }
    }

    public override void WriteLine(string? value)
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.AppendLine(value);
            FlushBuffer();
        }
    }

    // Optimized Write methods to avoid boxing and tuple allocations
    public void WriteFormatted(string format, object? arg0)
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.AppendFormat(format, arg0);
            CheckFlush();
        }
    }

    public void WriteFormatted(string format, object? arg0, object? arg1)
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.AppendFormat(format, arg0, arg1);
            CheckFlush();
        }
    }

    public void WriteFormatted(string format, object? arg0, object? arg1, object? arg2)
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.AppendFormat(format, arg0, arg1, arg2);
            CheckFlush();
        }
    }

    public void WriteFormatted(string format, params object?[] args)
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.AppendFormat(format, args);
            CheckFlush();
        }
    }

    public void WriteLineFormatted(string format, object? arg0)
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.AppendFormat(format, arg0);
            _buffer.AppendLine();
            FlushBuffer();
        }
    }

    public void WriteLineFormatted(string format, object? arg0, object? arg1)
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.AppendFormat(format, arg0, arg1);
            _buffer.AppendLine();
            FlushBuffer();
        }
    }

    public void WriteLineFormatted(string format, object? arg0, object? arg1, object? arg2)
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.AppendFormat(format, arg0, arg1, arg2);
            _buffer.AppendLine();
            FlushBuffer();
        }
    }

    public void WriteLineFormatted(string format, params object?[] args)
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            _buffer.AppendFormat(format, args);
            _buffer.AppendLine();
            FlushBuffer();
        }
    }

    public override void Flush()
    {
        lock (_lock)
        {
            FlushBuffer();
            _target.Flush();
        }
    }

    public override async Task FlushAsync()
    {
        string content;
        lock (_lock)
        {
            if (_buffer.Length == 0) return;
            
            content = _buffer.ToString();
            _buffer.Clear();
        }

        await _target.WriteAsync(content);
        await _target.FlushAsync();
    }

    private void CheckFlush()
    {
        // Flush if buffer is getting large
        if (_buffer.Length >= _bufferSize)
        {
            FlushBuffer();
        }
    }

    private void FlushBuffer()
    {
        if (_buffer.Length == 0) return;

        var content = _buffer.ToString();
        _buffer.Clear();
        _target.Write(content);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            lock (_lock)
            {
                FlushBuffer();
                _disposed = true;
            }
        }
        base.Dispose(disposing);
    }

#if NET
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await FlushAsync();
            lock (_lock)
            {
                _disposed = true;
            }
        }
        await base.DisposeAsync();
    }
#endif
}