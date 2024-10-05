namespace TUnit.Core;

public class BeforeTestDiscoveryContext : Context
{
    private static readonly AsyncLocal<BeforeTestDiscoveryContext?> Contexts = new();
    public new static BeforeTestDiscoveryContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }
    
    internal BeforeTestDiscoveryContext()
    {
    }
    
    public required string? TestFilter { get; init; }
}