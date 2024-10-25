using TUnit.Core.Logging;

namespace TUnit.Core;

public class GlobalContext : Context
{
    private static readonly AsyncLocal<GlobalContext?> Contexts = new();
    public new static GlobalContext Current
    {
        get
        {
            return Contexts.Value ??= new GlobalContext();
        }
        internal set => Contexts.Value = value;
    }
    
    internal GlobalContext()
    {
    }

    internal ILogger GlobalLogger { get; set; } = new NullLogger();
    
    public string? TestFilter { get; internal set; }
}