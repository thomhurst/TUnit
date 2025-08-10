using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;

namespace TUnit.Core;

public abstract class Context : IContext, IDisposable
{
    protected Context? Parent
    {
        get;
    }

    public static Context Current =>
        TestContext.Current as Context
        ?? ClassHookContext.Current as Context
        ?? AssemblyHookContext.Current as Context
        ?? TestSessionContext.Current as Context
        ?? BeforeTestDiscoveryContext.Current as Context
        ?? GlobalContext.Current;

    private readonly ConcurrentQueue<string> _outputSegments = new();
    private readonly ConcurrentQueue<string> _errorOutputSegments = new();
    private DefaultLogger? _defaultLogger;

    [field: AllowNull, MaybeNull]
    public TextWriter OutputWriter => field ??= new LockFreeStringWriter(_outputSegments);

    [field: AllowNull, MaybeNull]
    public TextWriter ErrorOutputWriter => field ??= new LockFreeStringWriter(_errorOutputSegments);

    internal Context(Context? parent)
    {
        Parent = parent;
    }

#if NET
    internal ExecutionContext? ExecutionContext { get; private set; }
#endif

    public void RestoreExecutionContext()
    {
#if NET
        if (ExecutionContext is not null)
        {
            ExecutionContext.Restore(ExecutionContext);
        }
        
        RestoreContextAsyncLocal();
#endif
    }

    internal abstract void RestoreContextAsyncLocal();

    public void AddAsyncLocalValues()
    {
#if NETSTANDARD
        throw new PlatformNotSupportedException("This method is not supported in .NET Standard - Please upgrade to .NET 8+.");
#else
        if (ExecutionContext.Capture() is {} executionContext)
        {
            ExecutionContext = executionContext;
        }
#endif
    }

    public string GetStandardOutput()
    {
        if (_outputSegments.IsEmpty)
            return string.Empty;
            
        var sb = new StringBuilder();
        foreach (var segment in _outputSegments)
        {
            sb.Append(segment);
        }
        return sb.ToString().Trim();
    }

    public string GetErrorOutput()
    {
        if (_errorOutputSegments.IsEmpty)
            return string.Empty;
            
        var sb = new StringBuilder();
        foreach (var segment in _errorOutputSegments)
        {
            sb.Append(segment);
        }
        return sb.ToString().Trim();
    }

    public DefaultLogger GetDefaultLogger()
    {
        return _defaultLogger ??= new DefaultLogger(this);
    }

    public void Dispose()
    {
#if NET
        ExecutionContext?.Dispose();
#endif
    }
}

/// <summary>
/// A lock-free TextWriter that uses a ConcurrentQueue for thread-safe writes without locks
/// </summary>
internal sealed class LockFreeStringWriter : TextWriter
{
    private readonly ConcurrentQueue<string> _segments;

    public LockFreeStringWriter(ConcurrentQueue<string> segments)
    {
        _segments = segments;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        _segments.Enqueue(value.ToString());
    }

    public override void Write(string? value)
    {
        if (value != null)
        {
            _segments.Enqueue(value);
        }
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer != null && count > 0)
        {
            _segments.Enqueue(new string(buffer, index, count));
        }
    }

    public override void WriteLine()
    {
        _segments.Enqueue(Environment.NewLine);
    }

    public override void WriteLine(string? value)
    {
        _segments.Enqueue((value ?? string.Empty) + Environment.NewLine);
    }

    public override void Write(char[]? buffer)
    {
        if (buffer != null)
        {
            _segments.Enqueue(new string(buffer));
        }
    }

    public override void Write(bool value)
    {
        _segments.Enqueue(value.ToString());
    }

    public override void Write(int value)
    {
        _segments.Enqueue(value.ToString());
    }

    public override void Write(uint value)
    {
        _segments.Enqueue(value.ToString());
    }

    public override void Write(long value)
    {
        _segments.Enqueue(value.ToString());
    }

    public override void Write(ulong value)
    {
        _segments.Enqueue(value.ToString());
    }

    public override void Write(float value)
    {
        _segments.Enqueue(value.ToString());
    }

    public override void Write(double value)
    {
        _segments.Enqueue(value.ToString());
    }

    public override void Write(decimal value)
    {
        _segments.Enqueue(value.ToString());
    }

    public override void Write(object? value)
    {
        if (value != null)
        {
            _segments.Enqueue(value.ToString() ?? string.Empty);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
