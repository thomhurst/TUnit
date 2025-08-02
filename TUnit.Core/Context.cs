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

    [field: AllowNull, MaybeNull]
    public TextWriter OutputWriter => field ??= TextWriter.Synchronized(new StringWriter(_outputStringBuilder ??= new StringBuilder()));

    [field: AllowNull, MaybeNull]
    public TextWriter ErrorOutputWriter => field ??= TextWriter.Synchronized(new StringWriter(_errorOutputStringBuilder ??= new StringBuilder()));

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
        return _outputStringBuilder?.ToString().Trim() ?? string.Empty;
    }

    public string GetErrorOutput()
    {
        return _errorOutputStringBuilder?.ToString().Trim() ?? string.Empty;
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
