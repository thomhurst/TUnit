using System.Text;
using TUnit.Engine.Services;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace TUnit.Engine.Logging;

/// <summary>
/// Optimized console interceptor that eliminates tuple allocations and uses buffered output
/// </summary>
internal abstract class OptimizedConsoleInterceptor : TextWriter
{
    private readonly VerbosityService _verbosityService;
    private readonly BufferedTextWriter? _originalOutBuffer;

    protected OptimizedConsoleInterceptor(VerbosityService verbosityService)
    {
        _verbosityService = verbosityService;
        
        var originalOut = GetOriginalOut();
        
        // Wrap outputs with buffered writers for better performance
        _originalOutBuffer = originalOut != null ? new BufferedTextWriter(originalOut, 2048) : null;
    }

    public override Encoding Encoding => RedirectedOut?.Encoding ?? _originalOutBuffer?.Encoding ?? Encoding.UTF8;

    protected abstract TextWriter? RedirectedOut { get; }

    private protected abstract TextWriter GetOriginalOut();

    private protected abstract void ResetDefault();

#if NET
    public override ValueTask DisposeAsync()
    {
        ResetDefault();
        _originalOutBuffer?.Dispose();
        // Don't dispose RedirectedOut as it's not owned by us
        return ValueTask.CompletedTask;
    }
#endif

    public override void Flush()
    {
        _originalOutBuffer?.Flush();
        RedirectedOut?.Flush();
    }

    public override async Task FlushAsync()
    {
        if (_originalOutBuffer != null)
            await _originalOutBuffer.FlushAsync();
        if (RedirectedOut != null)
            await RedirectedOut.FlushAsync();
    }

    // Optimized Write methods - no tuple allocations
    
    public override void Write(bool value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(value.ToString());
        RedirectedOut?.Write(value.ToString());
    }

    public override void Write(char value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(value);
        RedirectedOut?.Write(value);
    }

    public override void Write(char[]? buffer)
    {
        if (buffer == null) return;
        
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(buffer);
        RedirectedOut?.Write(buffer);
    }

    public override void Write(decimal value)
    {
        var str = value.ToString();
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(str);
        RedirectedOut?.Write(str);
    }

    public override void Write(double value)
    {
        var str = value.ToString();
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(str);
        RedirectedOut?.Write(str);
    }

    public override void Write(int value)
    {
        var str = value.ToString();
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(str);
        RedirectedOut?.Write(str);
    }

    public override void Write(long value)
    {
        var str = value.ToString();
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(str);
        RedirectedOut?.Write(str);
    }

    public override void Write(object? value)
    {
        var str = value?.ToString() ?? string.Empty;
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(str);
        RedirectedOut?.Write(str);
    }

    public override void Write(float value)
    {
        var str = value.ToString();
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(str);
        RedirectedOut?.Write(str);
    }

    public override void Write(string? value)
    {
        if (value == null) return;
        
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(value);
        RedirectedOut?.Write(value);
    }

    public override void Write(uint value)
    {
        var str = value.ToString();
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(str);
        RedirectedOut?.Write(str);
    }

    public override void Write(ulong value)
    {
        var str = value.ToString();
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(str);
        RedirectedOut?.Write(str);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(buffer, index, count);
        RedirectedOut?.Write(buffer, index, count);
    }

    // Optimized formatted Write methods - no tuple allocations
    public override void Write(string format, object? arg0)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteFormatted(format, arg0);
        RedirectedOut?.Write(format, arg0);
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteFormatted(format, arg0, arg1);
        RedirectedOut?.Write(format, arg0, arg1);
    }

    public override void Write(string format, object? arg0, object? arg1, object? arg2)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteFormatted(format, arg0, arg1, arg2);
        RedirectedOut?.Write(format, arg0, arg1, arg2);
    }

    public override void Write(string format, params object?[] arg)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteFormatted(format, arg);
        RedirectedOut?.Write(format, arg);
    }

    // WriteLine methods
    public override void WriteLine()
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine();
        RedirectedOut?.WriteLine();
    }

    public override void WriteLine(bool value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(value.ToString());
        RedirectedOut?.WriteLine(value.ToString());
    }

    public override void WriteLine(char value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(value.ToString());
        RedirectedOut?.WriteLine(value.ToString());
    }

    public override void WriteLine(char[]? buffer)
    {
        if (buffer == null) return;
        
        var str = new string(buffer);
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(str);
        RedirectedOut?.WriteLine(str);
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        var str = new string(buffer, index, count);
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(str);
        RedirectedOut?.WriteLine(str);
    }

    public override void WriteLine(decimal value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(value.ToString());
        RedirectedOut?.WriteLine(value.ToString());
    }

    public override void WriteLine(double value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(value.ToString());
        RedirectedOut?.WriteLine(value.ToString());
    }

    public override void WriteLine(int value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(value.ToString());
        RedirectedOut?.WriteLine(value.ToString());
    }

    public override void WriteLine(long value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(value.ToString());
        RedirectedOut?.WriteLine(value.ToString());
    }

    public override void WriteLine(object? value)
    {
        var str = value?.ToString() ?? string.Empty;
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(str);
        RedirectedOut?.WriteLine(str);
    }

    public override void WriteLine(float value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(value.ToString());
        RedirectedOut?.WriteLine(value.ToString());
    }

    public override void WriteLine(string? value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(value);
        RedirectedOut?.WriteLine(value);
    }

    public override void WriteLine(uint value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(value.ToString());
        RedirectedOut?.WriteLine(value.ToString());
    }

    public override void WriteLine(ulong value)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(value.ToString());
        RedirectedOut?.WriteLine(value.ToString());
    }

    // Optimized formatted WriteLine methods - no tuple allocations
    public override void WriteLine(string format, object? arg0)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLineFormatted(format, arg0);
        RedirectedOut?.WriteLine(format, arg0);
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLineFormatted(format, arg0, arg1);
        RedirectedOut?.WriteLine(format, arg0, arg1);
    }

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLineFormatted(format, arg0, arg1, arg2);
        RedirectedOut?.WriteLine(format, arg0, arg1, arg2);
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLineFormatted(format, arg);
        RedirectedOut?.WriteLine(format, arg);
    }

    // Async methods
    public override async Task WriteLineAsync()
    {
        if (!_verbosityService.HideTestOutput && _originalOutBuffer != null)
            await _originalOutBuffer.WriteAsync(Environment.NewLine);
        if (RedirectedOut != null)
            await RedirectedOut.WriteAsync(Environment.NewLine);
    }

    public override async Task WriteAsync(char value)
    {
        if (!_verbosityService.HideTestOutput && _originalOutBuffer != null)
            await _originalOutBuffer.WriteAsync(value.ToString());
        if (RedirectedOut != null)
            await RedirectedOut.WriteAsync(value.ToString());
    }

    public override async Task WriteAsync(char[] buffer, int index, int count)
    {
        var str = new string(buffer, index, count);
        if (!_verbosityService.HideTestOutput && _originalOutBuffer != null)
            await _originalOutBuffer.WriteAsync(str);
        if (RedirectedOut != null)
            await RedirectedOut.WriteAsync(str);
    }

    public override async Task WriteAsync(string? value)
    {
        if (value == null) return;
        
        if (!_verbosityService.HideTestOutput && _originalOutBuffer != null)
            await _originalOutBuffer.WriteAsync(value);
        if (RedirectedOut != null)
            await RedirectedOut.WriteAsync(value);
    }

    public override async Task WriteLineAsync(char value)
    {
        var str = value + Environment.NewLine;
        if (!_verbosityService.HideTestOutput && _originalOutBuffer != null)
            await _originalOutBuffer.WriteAsync(str);
        if (RedirectedOut != null)
            await RedirectedOut.WriteAsync(str);
    }

    public override async Task WriteLineAsync(char[] buffer, int index, int count)
    {
        var str = new string(buffer, index, count) + Environment.NewLine;
        if (!_verbosityService.HideTestOutput && _originalOutBuffer != null)
            await _originalOutBuffer.WriteAsync(str);
        if (RedirectedOut != null)
            await RedirectedOut.WriteAsync(str);
    }

    public override async Task WriteLineAsync(string? value)
    {
        var str = (value ?? string.Empty) + Environment.NewLine;
        if (!_verbosityService.HideTestOutput && _originalOutBuffer != null)
            await _originalOutBuffer.WriteAsync(str);
        if (RedirectedOut != null)
            await RedirectedOut.WriteAsync(str);
    }

#if NET
    public override void Write(ReadOnlySpan<char> buffer)
    {
        var str = new string(buffer);
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(str);
        RedirectedOut?.Write(str);
    }

    public override void Write(StringBuilder? value)
    {
        var str = value?.ToString() ?? string.Empty;
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.Write(str);
        RedirectedOut?.Write(str);
    }

    public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
    {
        var str = new string(buffer.Span);
        if (!_verbosityService.HideTestOutput && _originalOutBuffer != null)
            await _originalOutBuffer.WriteAsync(str);
        if (RedirectedOut != null)
            await RedirectedOut.WriteAsync(str);
    }

    public override async Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new())
    {
        var str = value?.ToString() ?? string.Empty;
        if (!_verbosityService.HideTestOutput && _originalOutBuffer != null)
            await _originalOutBuffer.WriteAsync(str);
        if (RedirectedOut != null)
            await RedirectedOut.WriteAsync(str);
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        var str = new string(buffer);
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(str);
        RedirectedOut?.WriteLine(str);
    }

    public override void WriteLine(StringBuilder? value)
    {
        var str = value?.ToString() ?? string.Empty;
        if (!_verbosityService.HideTestOutput)
            _originalOutBuffer?.WriteLine(str);
        RedirectedOut?.WriteLine(str);
    }

    public override async Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
    {
        var str = new string(buffer.Span) + Environment.NewLine;
        if (!_verbosityService.HideTestOutput && _originalOutBuffer != null)
            await _originalOutBuffer.WriteAsync(str);
        if (RedirectedOut != null)
            await RedirectedOut.WriteAsync(str);
    }

    public override async Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new())
    {
        var str = (value?.ToString() ?? string.Empty) + Environment.NewLine;
        if (!_verbosityService.HideTestOutput && _originalOutBuffer != null)
            await _originalOutBuffer.WriteAsync(str);
        if (RedirectedOut != null)
            await RedirectedOut.WriteAsync(str);
    }
#endif

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
        Flush();
        _originalOutBuffer?.Dispose();
        // Don't dispose RedirectedOut as it's not owned by us
        ResetDefault();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _originalOutBuffer?.Dispose();
            // Don't dispose RedirectedOut as it's not owned by us
        }
        base.Dispose(disposing);
    }
}