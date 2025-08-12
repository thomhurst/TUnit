using System.Text;
using System.Threading.Channels;

namespace TUnit.Engine.Logging;

/// <summary>
/// Lock-free asynchronous console writer that maintains message order
/// while eliminating contention between parallel tests
/// </summary>
internal sealed class AsyncConsoleWriter : TextWriter
{
    private readonly TextWriter _target;
    private readonly Channel<WriteCommand> _writeChannel;
    private readonly Task _processorTask;
    private readonly CancellationTokenSource _shutdownCts = new();
    private volatile bool _disposed;

    // Command types for the queue
    private enum CommandType
    {
        Write,
        WriteLine,
        Flush
    }

    private readonly struct WriteCommand
    {
        public CommandType Type { get; }
        public string? Text { get; }
        
        public WriteCommand(CommandType type, string? text = null)
        {
            Type = type;
            Text = text;
        }

        public static WriteCommand Write(string text) => new(CommandType.Write, text);
        public static WriteCommand WriteLine(string text) => new(CommandType.WriteLine, text);
        public static WriteCommand FlushCommand => new(CommandType.Flush);
    }

    public AsyncConsoleWriter(TextWriter target)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        
        // Create an unbounded channel for maximum throughput
        // Order is guaranteed by the channel
        _writeChannel = Channel.CreateUnbounded<WriteCommand>(new UnboundedChannelOptions
        {
            SingleWriter = false, // Multiple threads can write
            SingleReader = true,  // Single background task reads
            AllowSynchronousContinuations = false // Don't block writers
        });

        // Start the background processor
        _processorTask = Task.Run(ProcessWritesAsync);
    }

    public override Encoding Encoding => _target.Encoding;

    public override void Write(char value)
    {
        if (_disposed) return;
        Write(value.ToString());
    }

    public override void Write(string? value)
    {
        if (_disposed || string.IsNullOrEmpty(value)) return;
        
        // Non-blocking write to channel
        if (!_writeChannel.Writer.TryWrite(WriteCommand.Write(value!)))
        {
            // Channel is closed, write directly
            try
            {
                _target.Write(value);
            }
            catch
            {
                // Ignore write errors
            }
        }
    }

    public override void Write(char[]? buffer)
    {
        if (_disposed || buffer == null) return;
        Write(new string(buffer));
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (_disposed || buffer == null || count <= 0) return;
        Write(new string(buffer, index, count));
    }

    public override void WriteLine()
    {
        if (_disposed) return;
        WriteLine(string.Empty);
    }

    public override void WriteLine(string? value)
    {
        if (_disposed) return;
        
        // Non-blocking write to channel
        if (!_writeChannel.Writer.TryWrite(WriteCommand.WriteLine(value ?? string.Empty)))
        {
            // Channel is closed, write directly
            try
            {
                _target.WriteLine(value);
            }
            catch
            {
                // Ignore write errors
            }
        }
    }

    public override void Flush()
    {
        if (_disposed) return;
        
        // Queue a flush command
        if (!_writeChannel.Writer.TryWrite(WriteCommand.FlushCommand))
        {
            // Channel is closed, flush directly
            try
            {
                _target.Flush();
            }
            catch
            {
                // Ignore flush errors
            }
        }
    }

    public override async Task FlushAsync()
    {
        if (_disposed) return;
        
        // Queue a flush and wait a bit for it to process
        _writeChannel.Writer.TryWrite(WriteCommand.FlushCommand);
        await Task.Delay(10); // Small delay to allow flush to process
    }

    /// <summary>
    /// Background task that processes all writes in order
    /// </summary>
    private async Task ProcessWritesAsync()
    {
        var buffer = new StringBuilder(4096);
        var lastFlush = DateTime.UtcNow;
        const int flushIntervalMs = 50;

        try
        {
            await foreach (var command in _writeChannel.Reader.ReadAllAsync(_shutdownCts.Token))
            {
                switch (command.Type)
                {
                    case CommandType.Write:
                        buffer.Append(command.Text);
                        break;
                        
                    case CommandType.WriteLine:
                        buffer.AppendLine(command.Text);
                        break;
                        
                    case CommandType.Flush:
                        FlushBuffer(buffer);
                        lastFlush = DateTime.UtcNow;
                        continue;
                }

                // Batch writes for efficiency
                var shouldFlush = buffer.Length > 4096 || // Buffer is large
                                  (DateTime.UtcNow - lastFlush).TotalMilliseconds > flushIntervalMs; // Time based

                if (shouldFlush)
                {
                    FlushBuffer(buffer);
                    lastFlush = DateTime.UtcNow;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            // Final flush on shutdown
            if (buffer.Length > 0)
            {
                FlushBuffer(buffer);
            }
        }
    }

    private void FlushBuffer(StringBuilder buffer)
    {
        if (buffer.Length == 0) return;
        
        try
        {
            _target.Write(buffer.ToString());
            _target.Flush();
        }
        catch
        {
            // Ignore write errors
        }
        finally
        {
            buffer.Clear();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        if (disposing)
        {
            // Signal shutdown
            _writeChannel.Writer.TryComplete();
            _shutdownCts.Cancel();
            
            // Wait briefly for processor to finish
            try
            {
                _processorTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // Ignore
            }
            
            _shutdownCts.Dispose();
        }

        base.Dispose(disposing);
    }

    // Formatted write methods to match BufferedTextWriter interface
    public void WriteFormatted(string format, object? arg0)
    {
        if (_disposed) return;
        Write(string.Format(format, arg0));
    }

    public void WriteFormatted(string format, object? arg0, object? arg1)
    {
        if (_disposed) return;
        Write(string.Format(format, arg0, arg1));
    }

    public void WriteFormatted(string format, object? arg0, object? arg1, object? arg2)
    {
        if (_disposed) return;
        Write(string.Format(format, arg0, arg1, arg2));
    }

    public void WriteFormatted(string format, params object?[] args)
    {
        if (_disposed) return;
        Write(string.Format(format, args));
    }

    public void WriteLineFormatted(string format, object? arg0)
    {
        if (_disposed) return;
        WriteLine(string.Format(format, arg0));
    }

    public void WriteLineFormatted(string format, object? arg0, object? arg1)
    {
        if (_disposed) return;
        WriteLine(string.Format(format, arg0, arg1));
    }

    public void WriteLineFormatted(string format, object? arg0, object? arg1, object? arg2)
    {
        if (_disposed) return;
        WriteLine(string.Format(format, arg0, arg1, arg2));
    }

    public void WriteLineFormatted(string format, params object?[] args)
    {
        if (_disposed) return;
        WriteLine(string.Format(format, args));
    }

#if NET
    public override async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Signal shutdown
        _writeChannel.Writer.TryComplete();
        _shutdownCts.Cancel();
        
        // Wait for processor to finish
        try
        {
            await _processorTask.ConfigureAwait(false);
        }
        catch
        {
            // Ignore
        }
        
        _shutdownCts.Dispose();
        await base.DisposeAsync();
    }
#endif
}