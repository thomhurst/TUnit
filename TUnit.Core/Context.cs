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

    private StringBuilder? _outputStringBuilder;
    private StringBuilder? _errorOutputStringBuilder;
    private DefaultLogger? _defaultLogger;
    private readonly object _outputLock = new();
    private readonly object _errorLock = new();

    [field: AllowNull, MaybeNull]
    public TextWriter OutputWriter => field ??= new SynchronizedStringWriter(_outputStringBuilder ??= new StringBuilder(), _outputLock);

    [field: AllowNull, MaybeNull]
    public TextWriter ErrorOutputWriter => field ??= new SynchronizedStringWriter(_errorOutputStringBuilder ??= new StringBuilder(), _errorLock);

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
        RestoreContextAsyncLocal();
        
        Parent?.RestoreExecutionContext();

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
        lock (_outputLock)
        {
            return _outputStringBuilder?.ToString().Trim() ?? string.Empty;
        }
    }

    public string GetErrorOutput()
    {
        lock (_errorLock)
        {
            return _errorOutputStringBuilder?.ToString().Trim() ?? string.Empty;
        }
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
/// A TextWriter wrapper that provides thread-safe access to a StringBuilder
/// </summary>
internal sealed class SynchronizedStringWriter : TextWriter
{
    private readonly StringBuilder _stringBuilder;
    private readonly object _lock;

    public SynchronizedStringWriter(StringBuilder stringBuilder, object lockObject)
    {
        _stringBuilder = stringBuilder;
        _lock = lockObject;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        lock (_lock)
        {
            _stringBuilder.Append(value);
        }
    }

    public override void Write(string? value)
    {
        if (value != null)
        {
            lock (_lock)
            {
                _stringBuilder.Append(value);
            }
        }
    }

    public override void Write(char[] buffer, int index, int count)
    {
        lock (_lock)
        {
            _stringBuilder.Append(buffer, index, count);
        }
    }

    public override void WriteLine()
    {
        lock (_lock)
        {
            _stringBuilder.AppendLine();
        }
    }

    public override void WriteLine(string? value)
    {
        lock (_lock)
        {
            _stringBuilder.AppendLine(value);
        }
    }

    public override string ToString()
    {
        lock (_lock)
        {
            return _stringBuilder.ToString();
        }
    }

    protected override void Dispose(bool disposing)
    {
        // Nothing to dispose, StringBuilder doesn't implement IDisposable
        base.Dispose(disposing);
    }
}
