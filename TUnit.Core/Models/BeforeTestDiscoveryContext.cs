using TUnit.Core.Interfaces;
using TUnit.Core.Logging;

namespace TUnit.Core;

public class BeforeTestDiscoveryContext : Context
{
    private static readonly AsyncLocal<BeforeTestDiscoveryContext?> Contexts = new();
    public static BeforeTestDiscoveryContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }
    
    internal BeforeTestDiscoveryContext()
    {
    }
}

public class GlobalContext : Context
{
    public static GlobalContext Current { get; } = new GlobalContext();
    
    private GlobalContext()
    {
    }

    public static LogLevel LogLevel { get; internal set; } = LogLevel.Information;
}