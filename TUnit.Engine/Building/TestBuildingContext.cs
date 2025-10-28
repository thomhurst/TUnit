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
    ITestExecutionFilter? Filter
);
