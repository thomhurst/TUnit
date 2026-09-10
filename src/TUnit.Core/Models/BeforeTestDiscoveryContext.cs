using TUnit.Core.Settings;

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

    /// <summary>
    /// Programmatic settings for TUnit. Configure these here to establish project-level defaults
    /// before any tests are discovered or executed.
    /// </summary>
    public TUnitSettings Settings => TUnitSettings.Default;

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
