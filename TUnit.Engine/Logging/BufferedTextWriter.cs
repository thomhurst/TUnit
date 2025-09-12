using System.Collections.Concurrent;
using System.Text;
using System.Threading;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member

namespace TUnit.Engine.Logging;

/// <summary>
/// A thread-safe buffered text writer that reduces allocation overhead
/// Uses per-thread buffers to minimize lock contention
/// </summary>
internal sealed class BufferedTextWriter : TextWriter, IDisposable
{
    private readonly TextWriter _target;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly int _bufferSize;
    private readonly ThreadLocal<StringBuilder> _threadLocalBuffer;
    private readonly ConcurrentQueue<string> _flushQueue = new();
    private volatile bool _disposed;
    private readonly Timer _flushTimer;

    public BufferedTextWriter(TextWriter target, int bufferSize = 4096)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _bufferSize = bufferSize;
        _threadLocalBuffer = new ThreadLocal<StringBuilder>(() => new StringBuilder(bufferSize));
        
        // Auto-flush every 100ms to prevent data loss
        _flushTimer = new Timer(AutoFlush, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
    }

    public override Encoding Encoding => _target.Encoding;

    public override IFormatProvider FormatProvider => _target.FormatProvider;

    public override string NewLine
    {
        get => _target.NewLine;
        set
        {
            _lock.EnterWriteLock();
            try
            {
                _target.NewLine = value ?? Environment.NewLine;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public override void Write(char value)
    {
        if (_disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.Append(value);
        CheckFlush(buffer);
    }

    public override void Write(string? value)
    {
        if (string.IsNullOrEmpty(value) || _disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.Append(value);
        CheckFlush(buffer);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer == null || count <= 0 || _disposed)
        {
            return;
        }

        var localBuffer = _threadLocalBuffer.Value;
        if (localBuffer == null)
        {
            return;
        }

        localBuffer.Append(buffer, index, count);
        CheckFlush(localBuffer);
    }

    public override void WriteLine()
    {
        if (_disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.AppendLine();
        FlushBuffer(buffer);
    }

    public override void WriteLine(string? value)
    {
        if (_disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.AppendLine(value);
        FlushBuffer(buffer);
    }

    // Optimized Write methods to avoid boxing and tuple allocations
    public void WriteFormatted(string format, object? arg0)
    {
        if (_disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.AppendFormat(format, arg0);
        CheckFlush(buffer);
    }

    public void WriteFormatted(string format, object? arg0, object? arg1)
    {
        if (_disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.AppendFormat(format, arg0, arg1);
        CheckFlush(buffer);
    }

    public void WriteFormatted(string format, object? arg0, object? arg1, object? arg2)
    {
        if (_disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.AppendFormat(format, arg0, arg1, arg2);
        CheckFlush(buffer);
    }

    public void WriteFormatted(string format, params object?[] args)
    {
        if (_disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.AppendFormat(format, args);
        CheckFlush(buffer);
    }

    public void WriteLineFormatted(string format, object? arg0)
    {
        if (_disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.AppendFormat(format, arg0);
        buffer.AppendLine();
        FlushBuffer(buffer);
    }

    public void WriteLineFormatted(string format, object? arg0, object? arg1)
    {
        if (_disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.AppendFormat(format, arg0, arg1);
        buffer.AppendLine();
        FlushBuffer(buffer);
    }

    public void WriteLineFormatted(string format, object? arg0, object? arg1, object? arg2)
    {
        if (_disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.AppendFormat(format, arg0, arg1, arg2);
        buffer.AppendLine();
        FlushBuffer(buffer);
    }

    public void WriteLineFormatted(string format, params object?[] args)
    {
        if (_disposed)
        {
            return;
        }

        var buffer = _threadLocalBuffer.Value;
        if (buffer == null)
        {
            return;
        }

        buffer.AppendFormat(format, args);
        buffer.AppendLine();
        FlushBuffer(buffer);
    }

    public override void Flush()
    {
        // Flush all thread-local buffers
        FlushAllThreadBuffers();
        
        // Collect content to write without holding the lock
        var contentToWrite = new List<string>();
        
        _lock.EnterWriteLock();
        try
        {
            // Dequeue all content while holding the lock
            while (_flushQueue.TryDequeue(out var content))
            {
                contentToWrite.Add(content);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        
        // Write content and flush outside the lock to avoid deadlock
        foreach (var content in contentToWrite)
        {
            _target.Write(content);
        }
        _target.Flush();
    }

    public override async Task FlushAsync()
    {
        // Flush all thread-local buffers
        FlushAllThreadBuffers();
        
        var contentToWrite = new List<string>();
        
        _lock.EnterWriteLock();
        try
        {
            // Get all queued content
            while (_flushQueue.TryDequeue(out var content))
            {
                contentToWrite.Add(content);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        // Write all content asynchronously
        foreach (var content in contentToWrite)
        {
            await _target.WriteAsync(content);
        }
        
        await _target.FlushAsync();
    }

    private void CheckFlush(StringBuilder buffer)
    {
        // Flush if buffer is getting large
        if (buffer.Length >= _bufferSize)
        {
            FlushBuffer(buffer);
        }
    }

    private void FlushBuffer(StringBuilder buffer)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        var content = buffer.ToString();
        buffer.Clear();
        
        // Queue content for batch writing
        _flushQueue.Enqueue(content);
        
        // Process queue if it's getting large
        if (_flushQueue.Count > 10)
        {
            // Collect content to write without holding the lock
            var contentToWrite = new List<string>();
            
            _lock.EnterWriteLock();
            try
            {
                // Dequeue content while holding the lock
                while (_flushQueue.TryDequeue(out var queuedContent) && contentToWrite.Count < 20)
                {
                    contentToWrite.Add(queuedContent);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            
            // Write content outside the lock to avoid deadlock
            foreach (var contentItem in contentToWrite)
            {
                _target.Write(contentItem);
            }
        }
    }
    
    private void FlushAllThreadBuffers()
    {
        // This forces all thread-local buffers to be flushed
        // by accessing them from the current thread context
        var currentBuffer = _threadLocalBuffer.Value;
        if (currentBuffer?.Length > 0)
        {
            FlushBuffer(currentBuffer);
        }
    }
    
    private void ProcessFlushQueue()
    {
        // Process all queued content
        while (_flushQueue.TryDequeue(out var content))
        {
            _target.Write(content);
        }
    }
    
    private void AutoFlush(object? state)
    {
        if (_disposed)
        {
            return;
        }
        
        try
        {
            FlushAllThreadBuffers();
            
            // Collect content to write without holding the lock
            var contentToWrite = new List<string>();
            
            _lock.EnterWriteLock();
            try
            {
                // Dequeue all content while holding the lock
                while (_flushQueue.TryDequeue(out var content))
                {
                    contentToWrite.Add(content);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            
            // Write content outside the lock to avoid deadlock
            foreach (var content in contentToWrite)
            {
                _target.Write(content);
            }
        }
        catch
        {
            // Ignore errors in auto-flush to prevent crashes
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _flushTimer?.Dispose();
            FlushAllThreadBuffers();
            
            // Collect content to write without holding the lock
            var contentToWrite = new List<string>();
            
            _lock.EnterWriteLock();
            try
            {
                // Dequeue all content while holding the lock
                while (_flushQueue.TryDequeue(out var content))
                {
                    contentToWrite.Add(content);
                }
                _disposed = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            
            // Write content outside the lock
            foreach (var content in contentToWrite)
            {
                _target.Write(content);
            }
            
            _threadLocalBuffer?.Dispose();
            _lock?.Dispose();
        }
        base.Dispose(disposing);
    }

#if NET
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _flushTimer?.Dispose();
            await FlushAsync();
            
            _lock.EnterWriteLock();
            try
            {
                _disposed = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            
            _threadLocalBuffer?.Dispose();
            _lock?.Dispose();
        }
        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif
}