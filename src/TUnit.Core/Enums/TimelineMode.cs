namespace TUnit.Core.Enums;

/// <summary>
/// Controls how the HTML report renders the class-level execution timeline. Used by
/// <see cref="ClassTimelineAttribute"/>.
/// </summary>
public enum TimelineMode
{
    /// <summary>
    /// Drop test-case spans and their full subtrees from the class timeline, leaving only
    /// class-level infrastructure (suite, init/dispose). Same as the implicit default for
    /// classes without a <see cref="ClassTimelineAttribute"/>.
    /// </summary>
    Collapsed = 0,

    /// <summary>
    /// Render the union of every test-case span and its non-<c>test body</c> children on
    /// the class timeline. Surfaces multi-step <c>[DependsOn]</c> / BDD-style flows
    /// end-to-end at the class level.
    /// </summary>
    FullExecution = 1,
}
