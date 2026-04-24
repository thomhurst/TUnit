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
        ?? TestBuildContext.Current as Context
        ?? ClassHookContext.Current as Context
        ?? AssemblyHookContext.Current as Context
        ?? TestSessionContext.Current as Context
        ?? BeforeTestDiscoveryContext.Current as Context
        ?? GlobalContext.Current;

    // Lazy output state: avoid allocating StringBuilder + RWLS + ConsoleLineBuffer
    // for contexts that never receive output (the common case for most tests)
    private StringBuilder? _outputBuilder;
    private StringBuilder? _errorOutputBuilder;
    private ReaderWriterLockSlim? _outputLock;
    private ReaderWriterLockSlim? _errorOutputLock;
    private DefaultLogger? _defaultLogger;

    // Console interceptor line buffers for partial writes (Console.Write without newline)
    // These are stored per-context to prevent output mixing between parallel tests
    // ConsoleLineBuffer uses Lock internally for efficient synchronization
    private ConsoleLineBuffer? _consoleStdOutLineBuffer;
    private ConsoleLineBuffer? _consoleStdErrLineBuffer;

    // Set by the console interceptor on first write so TestCoordinator can skip the
    // two Console.Out/Err FlushAsync state machines per test when nothing was written.
    // volatile is cheap and sufficient — the flag only ever transitions false -> true.
    private volatile bool _consoleOutputCaptured;

    // Thread-safe: console interceptors may access from multiple threads.
    private StringBuilder GetOutputBuilder() =>
        LazyInitializer.EnsureInitialized(ref _outputBuilder)!;
    private StringBuilder GetErrorOutputBuilder() =>
        LazyInitializer.EnsureInitialized(ref _errorOutputBuilder)!;
    private ReaderWriterLockSlim GetOutputLock() =>
        LazyInitializer.EnsureInitialized(ref _outputLock, static () => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion))!;
    private ReaderWriterLockSlim GetErrorOutputLock() =>
        LazyInitializer.EnsureInitialized(ref _errorOutputLock, static () => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion))!;

    private ConcurrentStringWriter? _outputWriter;
    private ConcurrentStringWriter? _errorOutputWriter;
    private ConcurrentStringWriter GetOutputWriter() =>
        LazyInitializer.EnsureInitialized(ref _outputWriter, () => new ConcurrentStringWriter(GetOutputBuilder(), GetOutputLock()))!;
    private ConcurrentStringWriter GetErrorOutputWriter() =>
        LazyInitializer.EnsureInitialized(ref _errorOutputWriter, () => new ConcurrentStringWriter(GetErrorOutputBuilder(), GetErrorOutputLock()))!;

    public TextWriter OutputWriter => GetOutputWriter();

    public TextWriter ErrorOutputWriter => GetErrorOutputWriter();

    // Internal accessors for console interceptor line buffers
    internal ConsoleLineBuffer ConsoleStdOutLineBuffer =>
        LazyInitializer.EnsureInitialized(ref _consoleStdOutLineBuffer)!;

    internal ConsoleLineBuffer ConsoleStdErrLineBuffer =>
        LazyInitializer.EnsureInitialized(ref _consoleStdErrLineBuffer)!;

    internal bool HasCapturedConsoleOutput => _consoleOutputCaptured;

    internal void MarkConsoleOutputCaptured() => _consoleOutputCaptured = true;

    internal Context(Context? parent)
    {
        Parent = parent;
    }

#if NET
    internal System.Diagnostics.Activity? Activity { get; set; }
    internal ExecutionContext? ExecutionContext { get; private set; }
#endif

    public void RestoreExecutionContext()
    {
#if NET
        if (ExecutionContext is not null)
        {
            // ExecutionContext.Restore() restores ALL AsyncLocal values — including
            // Activity.Current. The captured ExecutionContext may contain a stale
            // (already-stopped) Activity from a previous hook/event receiver.
            // Save the current Activity and restore it after the EC restore to
            // prevent Activity chain corruption across parallel tests.
            var currentActivity = System.Diagnostics.Activity.Current;
            ExecutionContext.Restore(ExecutionContext);
            System.Diagnostics.Activity.Current = currentActivity;
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

    public virtual string GetStandardOutput() => _outputWriter?.GetContent() ?? string.Empty;

    public virtual string GetErrorOutput() => _errorOutputWriter?.GetContent() ?? string.Empty;

    // Fast path for callers that need to know whether anything was ever captured —
    // lets the result-building code skip the reader/writer lock acquisition entirely
    // for the (very common) case of a passing test with no output.
    internal virtual bool HasCapturedOutput => _outputWriter != null || _errorOutputWriter != null;

    public DefaultLogger GetDefaultLogger()
    {
        return _defaultLogger ??= new DefaultLogger(this);
    }

    public void Dispose()
    {
#if NET
        TUnitActivitySource.StopActivity(Activity);
        Activity = null;
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
    private const int MaxOutputLength = 1_048_576; // 1M chars (~2MB)

    // Trim to 75% of max to avoid re-trimming on every subsequent write
    private const int TrimTarget = MaxOutputLength * 3 / 4;

    private static readonly string TruncationNotice =
        $"[... output truncated — exceeded {MaxOutputLength:N0} character limit, showing most recent output ...]{Environment.NewLine}";

    private readonly StringBuilder _builder;
    private readonly ReaderWriterLockSlim _lock;
    private bool _truncated;

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
            TrimIfNeeded();
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
                TrimIfNeeded();
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
                TrimIfNeeded();
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
            TrimIfNeeded();
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
            TrimIfNeeded();
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
                TrimIfNeeded();
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
            TrimIfNeeded();
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
            TrimIfNeeded();
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
            TrimIfNeeded();
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
            TrimIfNeeded();
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
            TrimIfNeeded();
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
            TrimIfNeeded();
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
            TrimIfNeeded();
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
            TrimIfNeeded();
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
                TrimIfNeeded();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    private void TrimIfNeeded()
    {
        if (_builder.Length > MaxOutputLength)
        {
            var removeCount = _builder.Length - TrimTarget;

            // Avoid splitting a surrogate pair at the trim boundary
            if (removeCount > 0 && char.IsHighSurrogate(_builder[removeCount - 1]))
            {
                removeCount--;
            }

            _builder.Remove(0, removeCount);
            _truncated = true;
        }
    }

    internal string GetContent()
    {
        _lock.EnterReadLock();
        try
        {
            if (_builder.Length == 0)
            {
                return string.Empty;
            }

            var content = _builder.ToString();
            return _truncated ? string.Concat(TruncationNotice, content) : content;
        }
        finally
        {
            _lock.ExitReadLock();
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
