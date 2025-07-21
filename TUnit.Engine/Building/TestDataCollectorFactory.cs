using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Services;
using TUnit.Engine.Building.Collectors;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Discovery;

namespace TUnit.Engine.Building;

public static class TestDataCollectorFactory
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:Using member 'TUnit.Engine.Discovery.ReflectionTestDataCollector.ReflectionTestDataCollector()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code", Justification = "Reflection mode is explicitly chosen and cannot support trimming")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Using member 'TUnit.Engine.Discovery.ReflectionTestDataCollector.ReflectionTestDataCollector()' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling", Justification = "Reflection mode is explicitly chosen and cannot support AOT")]
    public static ITestDataCollector Create(TestExecutionMode? mode = null, Assembly[]? assembliesToScan = null)
    {
        var executionMode = mode ?? ModeDetector.Mode;

        return executionMode switch
        {
            TestExecutionMode.SourceGeneration => new AotTestDataCollector(),
            TestExecutionMode.Reflection => new ReflectionTestDataCollector(),
            _ => throw new NotSupportedException($"Test execution mode '{executionMode}' is not supported")
        };
    }

    /// Attempts AOT mode first, falls back to reflection if no source-generated tests found
    [UnconditionalSuppressMessage("Trimming", "IL2026:Using member 'TUnit.Engine.Discovery.ReflectionTestDataCollector.ReflectionTestDataCollector()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code", Justification = "Reflection mode is a fallback and cannot support trimming")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Using member 'TUnit.Engine.Discovery.ReflectionTestDataCollector.ReflectionTestDataCollector()' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling", Justification = "Reflection mode is a fallback and cannot support AOT")]
    public static async Task<ITestDataCollector> CreateAutoDetectAsync(string testSessionId, Assembly[]? assembliesToScan = null)
    {
        // Try AOT mode first (check if any tests were registered)
        var aotCollector = new AotTestDataCollector();
        var aotTests = await aotCollector.CollectTestsAsync(testSessionId);

        if (aotTests.Any())
        {
            // Tests were found via source generation
            return aotCollector;
        }

        // Fall back to reflection mode
        return new ReflectionTestDataCollector();
    }
}
