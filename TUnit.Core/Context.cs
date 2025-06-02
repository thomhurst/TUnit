using System.Diagnostics.CodeAnalysis;
using System.Text;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;

namespace TUnit.Core;

/// <summary>
/// Represents the base context for TUnit.
/// </summary>
public abstract class Context : IContext, IDisposable
{
    protected Context? Parent
    {
        get;
    }
    
    /// <summary>
    /// Gets the current context.
    /// </summary>
    public static Context Current =>
        TestContext.Current as Context
        ?? ClassHookContext.Current as Context
        ?? AssemblyHookContext.Current as Context
        ?? TestSessionContext.Current as Context
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
 
    /// <summary>
    /// Initializes a new instance of the <see cref="Context"/> class.
    /// </summary>
    internal Context(Context? parent)
    {
        Parent = parent;
    }

#if NET
    private List<ExecutionContext>? _executionContexts;
#endif

    internal ExecutionContext[] GetExecutionContexts()
    {
#if NET
        if (_executionContexts is null)
        {
            return [];
        }
        
        return _executionContexts.ToArray();
#else
        return [];
#endif
    }

    internal void RestoreExecutionContext()
    {
#if NET
        Parent?.RestoreExecutionContext();

        if (_executionContexts is null)
        {
            return;
        }
        
        foreach (var executionContext in _executionContexts)
        {
            ExecutionContext.Restore(executionContext);
        }
#endif
    }

    /// <summary>
    /// Adds async local values to the context.
    /// </summary>
    public void AddAsyncLocalValues()
    {
#if NETSTANDARD
        throw new PlatformNotSupportedException("This method is not supported in .NET Standard - Please upgrade to .NET 8+.");
#else
        if (ExecutionContext.Capture() is {} executionContext)
        {
            (_executionContexts ??= []).Add(executionContext);
        }
#endif
    }
    
    /// <summary>
    /// Gets the standard output.
    /// </summary>
    /// <returns>The standard output as a string.</returns>
    public string GetStandardOutput()
    {
        return _outputStringBuilder?.ToString().Trim() ?? string.Empty;
    }
    
    /// <summary>
    /// Gets the error output.
    /// </summary>
    /// <returns>The error output as a string.</returns>
    public string GetErrorOutput()
    {
        return _errorOutputStringBuilder?.ToString().Trim() ?? string.Empty;
    }
    
    /// <summary>
    /// Gets the default logger.
    /// </summary>
    /// <returns>A <see cref="TUnitLogger"/> instance.</returns>
    public DefaultLogger GetDefaultLogger()
    {
        return _defaultLogger ??= new DefaultLogger(this);
    }

    /// <summary>
    /// Disposes the context.
    /// </summary>
    public void Dispose()
    {
#if NET
        if (_executionContexts is null)
        {
            return;
        }
        
        foreach (var executionContext in _executionContexts)
        {
            executionContext.Dispose();
        }
#endif
    }
}