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
    
    public StringWriter OutputWriter { get; } = new ThreadSafeStringWriter();
    public StringWriter ErrorOutputWriter { get; } = new ThreadSafeStringWriter();
 
    internal Context()
    {
    }
    
    public string GetStandardOutput()
    {
        return OutputWriter.ToString().Trim();
    }
    
    public string GetErrorOutput()
    {
        return ErrorOutputWriter.ToString().Trim();
    }
    
    public TUnitLogger GetDefaultLogger()
    {
        return new DefaultLogger();
    }
}