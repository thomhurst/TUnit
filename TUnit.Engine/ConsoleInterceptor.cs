using System.Globalization;
using System.Text;
using Microsoft.Testing.Platform.Logging;
using TUnit.Core;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace TUnit.Engine;

internal class ConsoleInterceptor : TextWriter
{
    public static ConsoleInterceptor Instance { get; private set; } = null!;

    private readonly ILogger<ConsoleInterceptor>? _logger;
    public override Encoding Encoding => InnerWriter?.Encoding ?? Encoding.UTF8;

    public static TextWriter DefaultOut { get; }
    
    public StringWriter? InnerWriter => TestContext.Current?.OutputWriter;

    static ConsoleInterceptor()
    {
        DefaultOut = Console.Out;
    }

    public ConsoleInterceptor(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ConsoleInterceptor>();
        Instance = this;
    }
    
    public void Initialize()
    {
        Console.SetOut(this);
    }

    public void SetModule(TestContext testContext)
    {
        testContext.OnDispose = (_, _) =>
        {
            try
            {
                _logger?.LogInformation(testContext.GetConsoleOutput());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        };
    }
    
    public new void Dispose()
    {
        InnerWriter?.Dispose();
        Console.SetOut(DefaultOut);
    }

    public override async ValueTask DisposeAsync()
    {
        if(InnerWriter is not null)
        {
            await InnerWriter.DisposeAsync();
        }

        Console.SetOut(DefaultOut);
    }

    public override void Flush()
    {
        InnerWriter?.Flush();
    }

    public override void Write(bool value)
    {
        InnerWriter?.Write(value);
    }

    public override void Write(char[]? buffer)
    {
        InnerWriter?.Write(buffer);
    }

    public override void Write(decimal value)
    {
        InnerWriter?.Write(value);
    }

    public override void Write(double value)
    {
        InnerWriter?.Write(value);
    }

    public override void Write(int value)
    {
        InnerWriter?.Write(value);
    }

    public override void Write(long value)
    {
        InnerWriter?.Write(value);
    }

    public override void Write(object? value)
    {
        InnerWriter?.Write(value);
    }

    public override void Write(float value)
    {
        InnerWriter?.Write(value);
    }

    public override void Write(string format, object? arg0)
    {
        InnerWriter?.Write(format, arg0);
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        InnerWriter?.Write(format, arg0, arg1);
    }

    public override void Write(string format, object? arg0, object? arg1, object? arg2)
    {
        InnerWriter?.Write(format, arg0, arg1, arg2);
    }

    public override void Write(string format, params object?[] arg)
    {
        InnerWriter?.Write(format, arg);
    }

    public override void Write(uint value)
    {
        InnerWriter?.Write(value);
    }

    public override void Write(ulong value)
    {
        InnerWriter?.Write(value);
    }

    public new Task WriteAsync(char[]? buffer)
    {
        return InnerWriter?.WriteAsync(buffer) ?? Task.CompletedTask;
    }

    public override void WriteLine()
    {
        InnerWriter?.WriteLine();
    }

    public override void WriteLine(bool value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override void WriteLine(char value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override void WriteLine(char[]? buffer)
    {
        InnerWriter?.WriteLine(buffer);
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        InnerWriter?.WriteLine(buffer, index, count);
    }

    public override void WriteLine(decimal value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override void WriteLine(double value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override void WriteLine(int value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override void WriteLine(long value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override void WriteLine(object? value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override void WriteLine(float value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override void WriteLine(string? value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override void WriteLine(string format, object? arg0)
    {
        InnerWriter?.WriteLine(format, arg0);
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        InnerWriter?.WriteLine(format, arg0, arg1);
    }

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
    {
        InnerWriter?.WriteLine(format, arg0, arg1, arg2);
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        InnerWriter?.WriteLine(format, arg);
    }

    public override void WriteLine(uint value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override void WriteLine(ulong value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override Task WriteLineAsync()
    {
        return InnerWriter?.WriteLineAsync() ?? Task.CompletedTask;
    }

    public new Task WriteLineAsync(char[]? buffer)
    {
        return InnerWriter?.WriteLineAsync(buffer) ?? Task.CompletedTask;
    }

    public override IFormatProvider FormatProvider => InnerWriter?.FormatProvider ?? CultureInfo.CurrentCulture;

    public override string NewLine
    {
        get => InnerWriter?.NewLine ?? Environment.NewLine;
        set
        {
            if (InnerWriter is null)
            {
                return;
            }
            
            InnerWriter.NewLine = value;
        }
    }

    public override void Close()
    {
        InnerWriter?.Close();
    }

    public override Task FlushAsync()
    {
        return InnerWriter?.FlushAsync() ?? Task.CompletedTask;
    }

    public override void Write(char value)
    {
        InnerWriter?.Write(value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        InnerWriter?.Write(buffer, index, count);
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        InnerWriter?.Write(buffer);
    }

    public override void Write(string? value)
    {
        InnerWriter?.Write(value);
    }

    public override void Write(StringBuilder? value)
    {
        InnerWriter?.Write(value);
    }

    public override Task WriteAsync(char value)
    {
        return InnerWriter?.WriteAsync(value) ?? Task.CompletedTask;
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        return InnerWriter?.WriteAsync(buffer, index, count) ?? Task.CompletedTask;
    }

    public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
    {
        return InnerWriter?.WriteAsync(buffer, cancellationToken) ?? Task.CompletedTask;
    }

    public override Task WriteAsync(string? value)
    {
        return InnerWriter?.WriteAsync(value) ?? Task.CompletedTask;
    }

    public override Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new())
    {
        return InnerWriter?.WriteAsync(value, cancellationToken) ?? Task.CompletedTask;
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        InnerWriter?.WriteLine(buffer);
    }

    public override void WriteLine(StringBuilder? value)
    {
        InnerWriter?.WriteLine(value);
    }

    public override Task WriteLineAsync(char value)
    {
        return InnerWriter?.WriteLineAsync(value) ?? Task.CompletedTask;
    }

    public override Task WriteLineAsync(char[] buffer, int index, int count)
    {
        return InnerWriter?.WriteLineAsync(buffer, index, count) ?? Task.CompletedTask;
    }

    public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
    {
        return InnerWriter?.WriteLineAsync(buffer, cancellationToken) ?? Task.CompletedTask;
    }

    public override Task WriteLineAsync(string? value)
    {
        return InnerWriter?.WriteLineAsync(value) ?? Task.CompletedTask;
    }

    public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new())
    {
        return InnerWriter?.WriteLineAsync(value, cancellationToken) ?? Task.CompletedTask;
    }
}