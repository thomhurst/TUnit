using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Text;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;

namespace TUnit.Core;

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
 
    internal Context()
    {
    }

    internal ExecutionContext? ExecutionContext { get; set; }
    
    public void AddAsyncLocalValues()
    {
#if NETSTANDARD
        throw new PlatformNotSupportedException("This method is not supported in .NET Standard - Please upgrade to .NET 8+.");
#else
        ExecutionContext = ExecutionContext.Capture();
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
    
    public TUnitLogger GetDefaultLogger()
    {
        return new DefaultLogger();
    }

    public void Dispose()
    {
        ExecutionContext?.Dispose();
    }
}