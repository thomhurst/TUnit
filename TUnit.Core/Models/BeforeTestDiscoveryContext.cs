namespace TUnit.Core;

/// <summary>
/// Represents the context before test discovery.
/// </summary>
public class BeforeTestDiscoveryContext : Context
{
    private static readonly AsyncLocal<BeforeTestDiscoveryContext?> Contexts = new();

    /// <summary>
    /// Gets or sets the current before test discovery context.
    /// </summary>
    public static new BeforeTestDiscoveryContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }

    internal BeforeTestDiscoveryContext() : base(GlobalContext.Current)
    {
        Current = this;
    }


    public GlobalContext GlobalContext => (GlobalContext) Parent!;

    /// <summary>
    /// Gets or sets the test filter.
    /// </summary>
    public required string? TestFilter { get; init; }

    internal override void RestoreContextAsyncLocal()
    {
        Current = this;
    }
}
