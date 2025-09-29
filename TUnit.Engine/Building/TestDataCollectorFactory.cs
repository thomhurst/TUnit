using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Engine.Building.Collectors;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Discovery;

namespace TUnit.Engine.Building;

/// <summary>
/// Factory for creating test data collectors based on the execution mode.
///
/// TUnit supports two execution modes:
/// 1. Source Generation Mode (AOT-compatible): Uses compile-time generated metadata
/// 2. Reflection Mode (Legacy/Dynamic): Uses runtime reflection for discovery
///
/// The suppressions in this class document the intentional use of reflection
/// when source generation is not available or explicitly disabled.
/// </summary>
internal static class TestDataCollectorFactory
{
    /// <summary>
    /// Creates a test data collector based on the specified or detected mode.
    /// Source generation mode is preferred for AOT compatibility.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Using member 'TUnit.Engine.Discovery.ReflectionTestDataCollector.ReflectionTestDataCollector()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code", Justification = "Reflection mode is explicitly chosen and cannot support trimming")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Using member 'TUnit.Engine.Discovery.ReflectionTestDataCollector.ReflectionTestDataCollector()' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling", Justification = "Reflection mode is explicitly chosen and cannot support AOT")]
    public static ITestDataCollector Create(bool? useSourceGeneration = null, Assembly[]? assembliesToScan = null)
    {
        var isSourceGenerationEnabled = useSourceGeneration ?? SourceRegistrar.IsEnabled;

        if (isSourceGenerationEnabled)
        {
            return new AotTestDataCollector();
        }
        else
        {
            return new ReflectionTestDataCollector();
        }
    }

    /// <summary>
    /// Attempts AOT mode first, falls back to reflection if no source-generated tests found.
    /// This provides automatic mode selection for optimal performance and compatibility.
    /// </summary>
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
