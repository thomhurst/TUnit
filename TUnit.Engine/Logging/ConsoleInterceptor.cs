using System.Text;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Engine.CommandLineProviders;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace TUnit.Engine.Logging;

internal abstract class ConsoleInterceptor(ICommandLineOptions commandLineOptions) : TextWriter
{
    public override Encoding Encoding => RedirectedOut?.Encoding ?? Encoding.UTF8;

    protected abstract TextWriter? RedirectedOut { get; }
    
    protected private abstract TextWriter GetOriginalOut();
    
    protected private abstract void ResetDefault();

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

    public override void Write(bool value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override void Write(char[]? buffer)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(buffer);
        }

        RedirectedOut?.Write(buffer);
    }

    public override void Write(decimal value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override void Write(double value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override void Write(int value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override void Write(long value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override void Write(object? value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override void Write(float value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override void Write(string format, object? arg0)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(format, arg0);
        }

        RedirectedOut?.Write(format, arg0);
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(format, arg0, arg1);
        }

        RedirectedOut?.Write(format, arg0, arg1);
    }

    public override void Write(string format, object? arg0, object? arg1, object? arg2)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(format, arg0, arg1, arg2);
        }

        RedirectedOut?.Write(format, arg0, arg1, arg2);
    }

    public override void Write(string format, params object?[] arg)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(format, arg);
        }

        RedirectedOut?.Write(format, arg);
    }

    public override void Write(uint value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override void Write(ulong value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override void WriteLine()
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine();
        }

        RedirectedOut?.WriteLine();
    }

    public override void WriteLine(bool value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override void WriteLine(char value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override void WriteLine(char[]? buffer)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(buffer);
        }

        RedirectedOut?.WriteLine(buffer);
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(buffer, index, count);
        }

        RedirectedOut?.WriteLine(buffer, index, count);
    }

    public override void WriteLine(decimal value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override void WriteLine(double value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override void WriteLine(int value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override void WriteLine(long value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override void WriteLine(object? value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override void WriteLine(float value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override void WriteLine(string? value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override void WriteLine(string format, object? arg0)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(format, arg0);
        }

        RedirectedOut?.WriteLine(format, arg0);
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(format, arg0, arg1);
        }

        RedirectedOut?.WriteLine(format, arg0, arg1);
    }

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(format, arg0, arg1, arg2);
        }

        RedirectedOut?.WriteLine(format, arg0, arg1, arg2);
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(format, arg);
        }

        RedirectedOut?.WriteLine(format, arg);
    }

    public override void WriteLine(uint value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override void WriteLine(ulong value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override async Task WriteLineAsync()
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            await GetOriginalOut().WriteLineAsync();
        }

        await (RedirectedOut?.WriteLineAsync() ?? Task.CompletedTask);
    }

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

    public override void Write(char value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(buffer, index, count);
        }

        RedirectedOut?.Write(buffer, index, count);
    }

    public override void Write(string? value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override async Task WriteAsync(char value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            await GetOriginalOut().WriteAsync(value);
        }

        await (RedirectedOut?.WriteAsync(value) ?? Task.CompletedTask);
    }

    public override async Task WriteAsync(char[] buffer, int index, int count)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            await GetOriginalOut().WriteAsync(buffer, index, count);
        }

        await (RedirectedOut?.WriteAsync(buffer, index, count) ?? Task.CompletedTask);
    }

    public override async Task WriteAsync(string? value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            await GetOriginalOut().WriteAsync(value);
        }

        await (RedirectedOut?.WriteAsync(value) ?? Task.CompletedTask);
    }

#if NET
    public override void Write(ReadOnlySpan<char> buffer)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(buffer);
        }

        RedirectedOut?.Write(buffer);
    }

    public override void Write(StringBuilder? value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().Write(value);
        }

        RedirectedOut?.Write(value);
    }

    public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            await GetOriginalOut().WriteAsync(buffer, cancellationToken);
        }

        await (RedirectedOut?.WriteAsync(buffer, cancellationToken) ?? Task.CompletedTask);
    }

    public override async Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new())
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            await GetOriginalOut().WriteAsync(value, cancellationToken);
        }

        await (RedirectedOut?.WriteAsync(value, cancellationToken) ?? Task.CompletedTask);
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(buffer);
        }

        RedirectedOut?.WriteLine(buffer);
    }

    public override void WriteLine(StringBuilder? value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            GetOriginalOut().WriteLine(value);
        }

        RedirectedOut?.WriteLine(value);
    }

    public override async Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            await GetOriginalOut().WriteLineAsync(buffer, cancellationToken);
        }

        await (RedirectedOut?.WriteLineAsync(buffer, cancellationToken) ?? Task.CompletedTask);
    }

    public override async Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new())
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            await GetOriginalOut().WriteLineAsync(value, cancellationToken);
        }

        await (RedirectedOut?.WriteLineAsync(value, cancellationToken) ?? Task.CompletedTask);
    }
#endif

    public override async Task WriteLineAsync(char value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            await GetOriginalOut().WriteLineAsync(value);
        }

        await (RedirectedOut?.WriteLineAsync(value) ?? Task.CompletedTask);
    }

    public override async Task WriteLineAsync(char[] buffer, int index, int count)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            await GetOriginalOut().WriteLineAsync(buffer, index, count);
        }

        await (RedirectedOut?.WriteLineAsync(buffer, index, count) ?? Task.CompletedTask);
    }

    public override async Task WriteLineAsync(string? value)
    {
        if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
        {
            await GetOriginalOut().WriteLineAsync(value);
        }

        await (RedirectedOut?.WriteLineAsync(value) ?? Task.CompletedTask);
    }
}