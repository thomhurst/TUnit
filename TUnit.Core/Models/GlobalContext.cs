using TUnit.Core.Logging;

namespace TUnit.Core;

public class GlobalContext : Context
{
    private static readonly AsyncLocal<GlobalContext?> Contexts = new();
    public static new GlobalContext Current
    {
        get
        {
            return Contexts.Value ??= new GlobalContext();
        }
        internal set => Contexts.Value = value;
    }
    
    internal GlobalContext() : base(null)
    {
    }

    internal ILogger GlobalLogger { get; set; } = new NullLogger();
    
    public string? TestFilter { get; internal set; }
    public TextWriter OriginalConsoleOut { get; set; } = Console.Out;
    public TextWriter OriginalConsoleError { get; set; } = Console.Error;
    
    internal override void RestoreContextAsyncLocal()
    {
        Current = this;
    }
}