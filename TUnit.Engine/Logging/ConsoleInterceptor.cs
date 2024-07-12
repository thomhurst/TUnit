using System.Globalization;
using System.Text;
using TUnit.Core;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace TUnit.Engine.Logging;

internal abstract class ConsoleInterceptor : TextWriter
{
    public override Encoding Encoding => OutputWriter?.Encoding ?? Encoding.UTF8;
    
    public abstract StringWriter? OutputWriter { get; }

    public abstract void Initialize();

    public abstract void SetModule(TestContext testContext);
    
    private protected abstract void ResetDefault();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        
        if (OutputWriter is not null)
        {
            await OutputWriter.DisposeAsync();
        }
        
        ResetDefault();
    }
    
    public override void Flush()
    {
        OutputWriter?.Flush();
    }

    public override void Write(bool value)
    {
        OutputWriter?.Write(value);
    }

    public override void Write(char[]? buffer)
    {
        OutputWriter?.Write(buffer);
    }

    public override void Write(decimal value)
    {
        OutputWriter?.Write(value);
    }

    public override void Write(double value)
    {
        OutputWriter?.Write(value);
    }

    public override void Write(int value)
    {
        OutputWriter?.Write(value);
    }

    public override void Write(long value)
    {
        OutputWriter?.Write(value);
    }

    public override void Write(object? value)
    {
        OutputWriter?.Write(value);
    }

    public override void Write(float value)
    {
        OutputWriter?.Write(value);
    }

    public override void Write(string format, object? arg0)
    {
        OutputWriter?.Write(format, arg0);
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        OutputWriter?.Write(format, arg0, arg1);
    }

    public override void Write(string format, object? arg0, object? arg1, object? arg2)
    {
        OutputWriter?.Write(format, arg0, arg1, arg2);
    }

    public override void Write(string format, params object?[] arg)
    {
        OutputWriter?.Write(format, arg);
    }

    public override void Write(uint value)
    {
        OutputWriter?.Write(value);
    }

    public override void Write(ulong value)
    {
        OutputWriter?.Write(value);
    }

    public new Task WriteAsync(char[]? buffer)
    {
        return OutputWriter?.WriteAsync(buffer) ?? Task.CompletedTask;
    }

    public override void WriteLine()
    {
        OutputWriter?.WriteLine();
    }

    public override void WriteLine(bool value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override void WriteLine(char value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override void WriteLine(char[]? buffer)
    {
        OutputWriter?.WriteLine(buffer);
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        OutputWriter?.WriteLine(buffer, index, count);
    }

    public override void WriteLine(decimal value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override void WriteLine(double value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override void WriteLine(int value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override void WriteLine(long value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override void WriteLine(object? value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override void WriteLine(float value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override void WriteLine(string? value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override void WriteLine(string format, object? arg0)
    {
        OutputWriter?.WriteLine(format, arg0);
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        OutputWriter?.WriteLine(format, arg0, arg1);
    }

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
    {
        OutputWriter?.WriteLine(format, arg0, arg1, arg2);
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        OutputWriter?.WriteLine(format, arg);
    }

    public override void WriteLine(uint value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override void WriteLine(ulong value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override Task WriteLineAsync()
    {
        return OutputWriter?.WriteLineAsync() ?? Task.CompletedTask;
    }

    public new Task WriteLineAsync(char[]? buffer)
    {
        return OutputWriter?.WriteLineAsync(buffer) ?? Task.CompletedTask;
    }

    public override IFormatProvider FormatProvider => OutputWriter?.FormatProvider ?? CultureInfo.CurrentCulture;

    public override string NewLine
    {
        get => OutputWriter?.NewLine ?? Environment.NewLine;
        set
        {
            if (OutputWriter is null)
            {
                return;
            }
            
            OutputWriter.NewLine = value;
        }
    }

    public override void Close()
    {
        OutputWriter?.Close();
    }

    public override Task FlushAsync()
    {
        return OutputWriter?.FlushAsync() ?? Task.CompletedTask;
    }

    public override void Write(char value)
    {
        OutputWriter?.Write(value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        OutputWriter?.Write(buffer, index, count);
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        OutputWriter?.Write(buffer);
    }

    public override void Write(string? value)
    {
        OutputWriter?.Write(value);
    }

    public override void Write(StringBuilder? value)
    {
        OutputWriter?.Write(value);
    }

    public override Task WriteAsync(char value)
    {
        return OutputWriter?.WriteAsync(value) ?? Task.CompletedTask;
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        return OutputWriter?.WriteAsync(buffer, index, count) ?? Task.CompletedTask;
    }

    public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
    {
        return OutputWriter?.WriteAsync(buffer, cancellationToken) ?? Task.CompletedTask;
    }

    public override Task WriteAsync(string? value)
    {
        return OutputWriter?.WriteAsync(value) ?? Task.CompletedTask;
    }

    public override Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new())
    {
        return OutputWriter?.WriteAsync(value, cancellationToken) ?? Task.CompletedTask;
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        OutputWriter?.WriteLine(buffer);
    }

    public override void WriteLine(StringBuilder? value)
    {
        OutputWriter?.WriteLine(value);
    }

    public override Task WriteLineAsync(char value)
    {
        return OutputWriter?.WriteLineAsync(value) ?? Task.CompletedTask;
    }

    public override Task WriteLineAsync(char[] buffer, int index, int count)
    {
        return OutputWriter?.WriteLineAsync(buffer, index, count) ?? Task.CompletedTask;
    }

    public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
    {
        return OutputWriter?.WriteLineAsync(buffer, cancellationToken) ?? Task.CompletedTask;
    }

    public override Task WriteLineAsync(string? value)
    {
        return OutputWriter?.WriteLineAsync(value) ?? Task.CompletedTask;
    }

    public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new())
    {
        return OutputWriter?.WriteLineAsync(value, cancellationToken) ?? Task.CompletedTask;
    }
}