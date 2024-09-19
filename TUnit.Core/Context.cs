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
    
    public StringWriter OutputWriter { get; } = new();
    public StringWriter ErrorOutputWriter { get; } = new();
 
    internal Context()
    {
    }
    
    public string GetStandardOutput()
    {
        return OutputWriter.GetStringBuilder().ToString().Trim();
    }
    
    public string GetErrorOutput()
    {
        return ErrorOutputWriter.GetStringBuilder().ToString().Trim();
    }
    
    public TUnitLogger RegisterLogger(TUnitLogger logger)
    {
        return logger;
    }
    
    public TUnitLogger GetDefaultLogger()
    {
        return RegisterLogger(new DefaultLogger());
    }
}