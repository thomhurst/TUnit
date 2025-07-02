using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Services;
using TUnit.Engine.Building.Collectors;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building;

/// <summary>
/// Factory for creating the appropriate ITestDataCollector based on execution mode
/// </summary>
public static class TestDataCollectorFactory
{
    /// <summary>
    /// Creates an ITestDataCollector based on the current execution mode
    /// </summary>
    public static ITestDataCollector Create(TestExecutionMode? mode = null, Assembly[]? assembliesToScan = null)
    {
        var executionMode = mode ?? ModeDetector.Mode;

        return executionMode switch
        {
            TestExecutionMode.SourceGeneration => new AotTestDataCollector(),
            TestExecutionMode.Reflection => throw new NotSupportedException(
                "Reflection mode support is not yet implemented. " +
                "To add reflection support, implement ReflectionTestDataCollector : ITestDataCollector"),
            _ => throw new NotSupportedException($"Test execution mode '{executionMode}' is not supported")
        };
    }

    /// <summary>
    /// Creates an ITestDataCollector with automatic mode detection
    /// </summary>
    public static ITestDataCollector CreateAutoDetect(Assembly[]? assembliesToScan = null)
    {
        // For now, always use AOT mode until reflection support is implemented
        return new AotTestDataCollector();
    }
}