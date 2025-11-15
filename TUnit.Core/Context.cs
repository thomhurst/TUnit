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

    public static Context Current
    {
        get
        {
            // Try TestContext first (most specific)
            if (TestContext.Current is Context testContext)
            {
                return testContext;
            }

            // Then try hook contexts
            if (ClassHookContext.Current is Context classHookContext)
            {
                return classHookContext;
            }

            if (AssemblyHookContext.Current is Context assemblyHookContext)
            {
                return assemblyHookContext;
            }

            // Check TestBuilderContext BEFORE TestSessionContext
            // TestBuilderContext is more specific (building data for a specific test)
            // and should take precedence during data source construction
            if (TestBuilderContext.Current is { } builderContext)
            {
                return new TestBuilderContextWrapper(builderContext);
            }

            if (TestSessionContext.Current is Context testSessionContext)
            {
                return testSessionContext;
            }

            if (BeforeTestDiscoveryContext.Current is Context beforeTestDiscoveryContext)
            {
                return beforeTestDiscoveryContext;
            }

            // Fall back to GlobalContext
            return GlobalContext.Current;
        }
    }

    private readonly StringBuilder _outputBuilder = new();
    private readonly StringBuilder _errorOutputBuilder = new();
    private readonly ReaderWriterLockSlim _outputLock = new(LockRecursionPolicy.NoRecursion);
    private readonly ReaderWriterLockSlim _errorOutputLock = new(LockRecursionPolicy.NoRecursion);
    private DefaultLogger? _defaultLogger;

    [field: AllowNull, MaybeNull]
    public virtual TextWriter OutputWriter => field ??= new ConcurrentStringWriter(_outputBuilder, _outputLock);

    [field: AllowNull, MaybeNull]
    public virtual TextWriter ErrorOutputWriter => field ??= new ConcurrentStringWriter(_errorOutputBuilder, _errorOutputLock);

    internal Context(Context? parent)
    {
        Parent = parent;
    }

    /// <summary>
    /// Transfers captured output from a TestBuilderContext to this Context.
    /// This is used to preserve console output that was captured during test data construction.
    /// </summary>
    internal void TransferOutputFrom(TestBuilderContext builderContext)
    {
        var capturedOutput = builderContext.GetCapturedOutput();
        if (!string.IsNullOrEmpty(capturedOutput))
        {
            _outputLock.EnterWriteLock();
            try
            {
                // Insert at the beginning so build-time output comes before test-time output
                _outputBuilder.Insert(0, capturedOutput);
            }
            finally
            {
                _outputLock.ExitWriteLock();
            }
        }

        var capturedErrorOutput = builderContext.GetCapturedErrorOutput();
        if (!string.IsNullOrEmpty(capturedErrorOutput))
        {
            _errorOutputLock.EnterWriteLock();
            try
            {
                _errorOutputBuilder.Insert(0, capturedErrorOutput);
            }
            finally
            {
                _errorOutputLock.ExitWriteLock();
            }
        }
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

        SetAsyncLocalContext();
#endif
    }

    internal abstract void SetAsyncLocalContext();

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
        if (_outputBuilder.Length == 0)
        {
            return string.Empty;
        }
        
        _outputLock.EnterReadLock();

        try
        {
            return _outputBuilder.ToString().Trim();
        }
        finally
        {
            _outputLock.ExitReadLock();
        }
    }

    public string GetErrorOutput()
    {
        if (_errorOutputBuilder.Length == 0)
        {
            return string.Empty;
        }

        _errorOutputLock.EnterReadLock();

        try
        {
            return _errorOutputBuilder.ToString().Trim();
        }
        finally
        {
            _errorOutputLock.ExitReadLock();
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
        _outputLock?.Dispose();
        _errorOutputLock?.Dispose();
    }
}

/// <summary>
/// A concurrent TextWriter implementation that provides thread-safe access to a StringBuilder
/// </summary>
internal sealed class ConcurrentStringWriter : TextWriter
{
    private readonly StringBuilder _builder;
    private readonly ReaderWriterLockSlim _lock;

    public ConcurrentStringWriter(StringBuilder builder, ReaderWriterLockSlim lockSlim)
    {
        _builder = builder;
        _lock = lockSlim;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        _lock.EnterWriteLock();
        try
        {
            _builder.Append(value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void Write(string? value)
    {
        if (value != null)
        {
            _lock.EnterWriteLock();
            try
            {
                _builder.Append(value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer != null && count > 0)
        {
            _lock.EnterWriteLock();
            try
            {
                _builder.Append(buffer, index, count);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public override void WriteLine()
    {
        _lock.EnterWriteLock();
        try
        {
            _builder.AppendLine();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void WriteLine(string? value)
    {
        _lock.EnterWriteLock();
        try
        {
            _builder.AppendLine(value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void Write(char[]? buffer)
    {
        if (buffer != null)
        {
            _lock.EnterWriteLock();
            try
            {
                _builder.Append(buffer);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public override void Write(bool value)
    {
        _lock.EnterWriteLock();
        try
        {
            _builder.Append(value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void Write(int value)
    {
        _lock.EnterWriteLock();
        try
        {
            _builder.Append(value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void Write(uint value)
    {
        _lock.EnterWriteLock();
        try
        {
            _builder.Append(value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void Write(long value)
    {
        _lock.EnterWriteLock();
        try
        {
            _builder.Append(value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void Write(ulong value)
    {
        _lock.EnterWriteLock();
        try
        {
            _builder.Append(value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void Write(float value)
    {
        _lock.EnterWriteLock();
        try
        {
            _builder.Append(value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void Write(double value)
    {
        _lock.EnterWriteLock();
        try
        {
            _builder.Append(value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void Write(decimal value)
    {
        _lock.EnterWriteLock();
        try
        {
            _builder.Append(value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void Write(object? value)
    {
        if (value != null)
        {
            _lock.EnterWriteLock();
            try
            {
                _builder.Append(value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public override void Flush()
    {
        // StringBuilder doesn't need flushing
    }

    protected override void Dispose(bool disposing)
    {
        // Don't dispose the lock or builder - they're owned by Context
        base.Dispose(disposing);
    }
}

/// <summary>
/// Wraps a TestBuilderContext to provide a Context interface for console output capture
/// during test data construction.
/// </summary>
internal sealed class TestBuilderContextWrapper : Context
{
    private readonly TestBuilderContext _builderContext;

    public TestBuilderContextWrapper(TestBuilderContext builderContext) : base(null)
    {
        _builderContext = builderContext;
    }

    public override TextWriter OutputWriter => _builderContext.OutputWriter;

    public override TextWriter ErrorOutputWriter => _builderContext.ErrorOutputWriter;

    internal override void SetAsyncLocalContext()
    {
        // TestBuilderContext manages its own async local, so this is a no-op
    }
}
