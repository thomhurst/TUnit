using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Factory for creating test discovery implementations based on execution mode.
/// </summary>
public static class TestDiscoveryFactory
{
    /// <summary>
    /// Creates a test discovery implementation based on the specified mode.
    /// </summary>
    /// <param name="mode">The test execution mode.</param>
    /// <param name="assembliesToScan">Optional assemblies to check for source-generated tests.</param>
    /// <returns>An appropriate test discovery implementation.</returns>
    public static ITestDiscovery Create(TestExecutionMode mode, Assembly[]? assembliesToScan = null)
    {
        return mode switch
        {
            TestExecutionMode.SourceGeneration => new AotTestDiscovery(),
            TestExecutionMode.Reflection => CreateReflectionDiscovery(),
            TestExecutionMode.Auto => TryCreateAotDiscovery(assembliesToScan) ?? CreateReflectionDiscovery(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid test execution mode")
        };
    }
    
    /// <summary>
    /// Creates a reflection-based test discovery implementation.
    /// This method is not AOT-compatible.
    /// </summary>
    [RequiresUnreferencedCode("Reflection mode is not AOT-compatible")]
    [RequiresDynamicCode("Reflection mode requires runtime code generation")]
    private static ITestDiscovery CreateReflectionDiscovery()
    {
        return new ReflectionTestDiscovery();
    }
    
    /// <summary>
    /// Attempts to create an AOT test discovery if source-generated tests are available.
    /// </summary>
    private static ITestDiscovery? TryCreateAotDiscovery(Assembly[]? assembliesToScan)
    {
        if (assembliesToScan == null || assembliesToScan.Length == 0)
        {
            return null;
        }
        
        // Check if any assembly has source-generated tests
        var hasSourceGeneratedTests = assembliesToScan.Any(AotTestDiscovery.HasSourceGeneratedTests);
        
        if (hasSourceGeneratedTests)
        {
            return new AotTestDiscovery();
        }
        
        return null;
    }
    
    /// <summary>
    /// Determines the best execution mode based on available implementations.
    /// </summary>
    public static TestExecutionMode DetermineMode(Assembly[]? assembliesToScan = null)
    {
        if (assembliesToScan != null && assembliesToScan.Any(AotTestDiscovery.HasSourceGeneratedTests))
        {
            return TestExecutionMode.SourceGeneration;
        }
        
        return TestExecutionMode.Reflection;
    }
}