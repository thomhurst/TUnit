using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;

namespace TUnit.Core;

public abstract class Context : IContext
{
    public static Context Current =>
        TestContext.Current as Context
        ?? ClassHookContext.Current as Context
        ?? AssemblyHookContext.Current as Context
        ?? TestSessionContext.Current as Context
        ?? TestDiscoveryContext.Current as Context
        ?? BeforeTestDiscoveryContext.Current as Context
        ?? GlobalContext.Current;
    
    [field: AllowNull, MaybeNull]
    public TextWriter OutputWriter => field ??= TextWriter.Synchronized(new StringWriter());
    
    [field: AllowNull, MaybeNull]
    public TextWriter ErrorOutputWriter => field ??= TextWriter.Synchronized(new StringWriter());
 
    internal Context()
    {
    }
    
    public string GetStandardOutput()
    {
        return OutputWriter.ToString()?.Trim() ?? string.Empty;
    }
    
    public string GetErrorOutput()
    {
        return ErrorOutputWriter.ToString()?.Trim() ?? string.Empty;
    }
    
    public TUnitLogger GetDefaultLogger()
    {
        return new DefaultLogger();
    }
}