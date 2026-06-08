using Microsoft.Testing.Platform.Requests;

namespace TUnit.Engine.Building;

/// <summary>
/// Context information for building tests, used to optimize test discovery and execution.
/// </summary>
internal record TestBuildingContext(
    /// <summary>
    /// Indicates whether tests are being built for execution (true) or discovery/display (false).
    /// When true, optimizations like early filtering can be applied.
    /// </summary>
    bool IsForExecution,

    /// <summary>
    /// The filter to apply during test building. Only relevant when IsForExecution is true.
    /// </summary>
    ITestExecutionFilter? Filter,

    /// <summary>
    /// When true, data sources marked with <c>DeferEnumeration</c> are expanded eagerly instead of
    /// producing a single placeholder node. Set during runtime expansion of a deferred placeholder so
    /// the real test cases are built (see <c>DeferredTestExpander</c>).
    /// </summary>
    bool IgnoreDeferral = false
);
