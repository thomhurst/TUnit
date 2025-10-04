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
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Falls back to reflection mode if no source-generated tests found")]
    [RequiresDynamicCode("Falls back to reflection mode if no source-generated tests found")]
    #endif
    public static async Task<ITestDataCollector> CreateAutoDetectAsync(string testSessionId, Assembly[]? assembliesToScan = null)
    {
        // Try AOT mode first (check if any tests were registered)
        var aotCollector = new AotTestDataCollector();
        #if NET6_0_OR_GREATER
        #pragma warning disable IL2026, IL3050 // AOT collector handles dynamic tests conditionally
        #endif
        var aotTests = await aotCollector.CollectTestsAsync(testSessionId);
        #if NET6_0_OR_GREATER
        #pragma warning restore IL2026, IL3050
        #endif

        if (aotTests.Any())
        {
            // Tests were found via source generation
            return aotCollector;
        }

        // Fall back to reflection mode
        return new ReflectionTestDataCollector();
    }
}
