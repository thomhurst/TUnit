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
    public static Context Current =>
        TestContext.Current as Context
        ?? ClassHookContext.Current as Context
        ?? AssemblyHookContext.Current as Context
        ?? TestSessionContext.Current as Context
        ?? TestDiscoveryContext.Current as Context
        ?? BeforeTestDiscoveryContext.Current as Context
        ?? GlobalContext.Current;

    private StringBuilder? _outputStringBuilder;
    private StringBuilder? _errorOutputStringBuilder;
    
    [field: AllowNull, MaybeNull]
    public TextWriter OutputWriter => field ??= TextWriter.Synchronized(new StringWriter(_outputStringBuilder ??= new StringBuilder()));
    
    [field: AllowNull, MaybeNull]
    public TextWriter ErrorOutputWriter => field ??= TextWriter.Synchronized(new StringWriter(_errorOutputStringBuilder ??= new StringBuilder()));
 
    /// <summary>
    /// Initializes a new instance of the <see cref="Context"/> class.
    /// </summary>
    internal Context()
    {
    }

    internal ExecutionContext? ExecutionContext { get; set; }
    
    /// <summary>
    /// Adds async local values to the context.
    /// </summary>
    public void AddAsyncLocalValues()
    {
#if NETSTANDARD
        throw new PlatformNotSupportedException("This method is not supported in .NET Standard - Please upgrade to .NET 8+.");
#else
        ExecutionContext = ExecutionContext.Capture();
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
    public TUnitLogger GetDefaultLogger()
    {
        return new DefaultLogger();
    }

    /// <summary>
    /// Disposes the context.
    /// </summary>
    public void Dispose()
    {
        ExecutionContext?.Dispose();
    }
}