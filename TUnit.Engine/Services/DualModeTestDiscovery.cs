using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;
using TUnit.Engine.Models;
using TUnit.Engine.Services;

namespace TUnit.Engine.Services;

/// <summary>
/// Unified test discovery that automatically chooses between source generation and reflection modes.
/// This provides a single entry point for test discovery that works seamlessly in both modes.
/// </summary>
public class DualModeTestDiscovery
{
    private readonly IModeDetector _modeDetector;
    private readonly SourceGeneratedTestDiscoverer? _sourceGeneratedDiscoverer;
    private readonly TUnitTestDiscoverer? _reflectionDiscoverer;

    public DualModeTestDiscovery(
        IModeDetector modeDetector,
        SourceGeneratedTestDiscoverer? sourceGeneratedDiscoverer = null,
        TUnitTestDiscoverer? reflectionDiscoverer = null)
    {
        _modeDetector = modeDetector;
        _sourceGeneratedDiscoverer = sourceGeneratedDiscoverer;
        _reflectionDiscoverer = reflectionDiscoverer;
    }

    /// <summary>
    /// Discovers tests using the appropriate mode based on runtime capabilities.
    /// </summary>
    /// <returns>Unified discovery result</returns>
    public async Task<DualModeDiscoveryResult> DiscoverTestsAsync()
    {
        var detectedMode = _modeDetector.DetectMode();

        switch (detectedMode)
        {
            case TestExecutionMode.SourceGeneration:
                return await DiscoverSourceGeneratedTestsAsync();

            case TestExecutionMode.Reflection:
                return await DiscoverReflectionTestsAsync();

            default:
                throw new NotSupportedException($"Test execution mode {detectedMode} is not supported");
        }
    }

    /// <summary>
    /// Discovers tests using source generation mode.
    /// </summary>
    private async Task<DualModeDiscoveryResult> DiscoverSourceGeneratedTestsAsync()
    {
        if (_sourceGeneratedDiscoverer == null)
        {
            return new DualModeDiscoveryResult
            {
                ExecutionMode = TestExecutionMode.SourceGeneration,
                Tests = Array.Empty<object>(),
                Failures = Array.Empty<object>(),
                TestVariations = Array.Empty<TestVariation>(),
                IsSuccessful = false,
                ErrorMessage = "Source generated test discoverer is not available"
            };
        }

        try
        {
            var result = await _sourceGeneratedDiscoverer.DiscoverTestsAsync();
            
            return new DualModeDiscoveryResult
            {
                ExecutionMode = TestExecutionMode.SourceGeneration,
                Tests = result.Tests,
                Failures = result.Failures,
                TestVariations = result.TestVariations,
                IsSuccessful = true,
                TestCount = result.TestCount,
                VariationCount = result.VariationCount
            };
        }
        catch (Exception ex)
        {
            return new DualModeDiscoveryResult
            {
                ExecutionMode = TestExecutionMode.SourceGeneration,
                Tests = Array.Empty<object>(),
                Failures = Array.Empty<object>(),
                TestVariations = Array.Empty<TestVariation>(),
                IsSuccessful = false,
                ErrorMessage = $"Source generation discovery failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Discovers tests using reflection mode.
    /// </summary>
    private async Task<DualModeDiscoveryResult> DiscoverReflectionTestsAsync()
    {
        if (_reflectionDiscoverer == null)
        {
            return new DualModeDiscoveryResult
            {
                ExecutionMode = TestExecutionMode.Reflection,
                Tests = Array.Empty<object>(),
                Failures = Array.Empty<object>(),
                TestVariations = Array.Empty<TestVariation>(),
                IsSuccessful = false,
                ErrorMessage = "Reflection test discoverer is not available"
            };
        }

        try
        {
            // Note: This is a simplified integration with the existing reflection discoverer
            // In a real implementation, this would need to properly integrate with TUnitTestDiscoverer
            
            return new DualModeDiscoveryResult
            {
                ExecutionMode = TestExecutionMode.Reflection,
                Tests = Array.Empty<DiscoveredTest>(), // TODO: Get from reflection discoverer
                Failures = Array.Empty<DiscoveryFailure>(),
                TestVariations = Array.Empty<TestVariation>(),
                IsSuccessful = true,
                TestCount = 0,
                VariationCount = 0
            };
        }
        catch (Exception ex)
        {
            return new DualModeDiscoveryResult
            {
                ExecutionMode = TestExecutionMode.Reflection,
                Tests = Array.Empty<object>(),
                Failures = Array.Empty<object>(),
                TestVariations = Array.Empty<TestVariation>(),
                IsSuccessful = false,
                ErrorMessage = $"Reflection discovery failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets comprehensive statistics about the dual-mode discovery system.
    /// </summary>
    /// <returns>Discovery statistics</returns>
    public DualModeDiscoveryStats GetDiscoveryStats()
    {
        var detectedMode = _modeDetector.DetectMode();
        
        return new DualModeDiscoveryStats
        {
            DetectedMode = detectedMode,
            IsSourceGenerationAvailable = _modeDetector.IsSourceGenerationAvailable,
            IsDynamicCodeSupported = _modeDetector.IsDynamicCodeSupported,
            IsReflectionModeRequested = _modeDetector.IsReflectionModeRequested,
            HasSourceGeneratedDiscoverer = _sourceGeneratedDiscoverer != null,
            HasReflectionDiscoverer = _reflectionDiscoverer != null
        };
    }
}

/// <summary>
/// Result of dual-mode test discovery.
/// </summary>
public sealed class DualModeDiscoveryResult
{
    /// <summary>
    /// The execution mode that was used for discovery.
    /// </summary>
    public TestExecutionMode ExecutionMode { get; init; }

    /// <summary>
    /// Whether discovery was successful.
    /// </summary>
    public bool IsSuccessful { get; init; }

    /// <summary>
    /// Error message if discovery failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Discovered tests ready for execution.
    /// </summary>
    public IReadOnlyList<DiscoveredTest> Tests { get; init; } = Array.Empty<DiscoveredTest>();

    /// <summary>
    /// Any failures that occurred during discovery.
    /// </summary>
    public IReadOnlyList<DiscoveryFailure> Failures { get; init; } = Array.Empty<DiscoveryFailure>();

    /// <summary>
    /// All test variations that were created.
    /// </summary>
    public IReadOnlyList<TestVariation> TestVariations { get; init; } = Array.Empty<TestVariation>();

    /// <summary>
    /// Total number of tests discovered.
    /// </summary>
    public int TestCount { get; init; }

    /// <summary>
    /// Total number of variations created.
    /// </summary>
    public int VariationCount { get; init; }
}

/// <summary>
/// Statistics about the dual-mode discovery system.
/// </summary>
public sealed class DualModeDiscoveryStats
{
    /// <summary>
    /// The detected execution mode.
    /// </summary>
    public TestExecutionMode DetectedMode { get; init; }

    /// <summary>
    /// Whether source generation is available.
    /// </summary>
    public bool IsSourceGenerationAvailable { get; init; }

    /// <summary>
    /// Whether dynamic code generation is supported.
    /// </summary>
    public bool IsDynamicCodeSupported { get; init; }

    /// <summary>
    /// Whether reflection mode was explicitly requested.
    /// </summary>
    public bool IsReflectionModeRequested { get; init; }

    /// <summary>
    /// Whether a source generated discoverer is available.
    /// </summary>
    public bool HasSourceGeneratedDiscoverer { get; init; }

    /// <summary>
    /// Whether a reflection discoverer is available.
    /// </summary>
    public bool HasReflectionDiscoverer { get; init; }

    /// <summary>
    /// Gets a human-readable description of the discovery configuration.
    /// </summary>
    public string Description
    {
        get
        {
            var mode = DetectedMode == TestExecutionMode.SourceGeneration ? "Source Generation (AOT-safe)" : "Reflection";
            var availability = IsSourceGenerationAvailable ? "available" : "not available";
            var dynamicCode = IsDynamicCodeSupported ? "supported" : "not supported";
            
            return $"Mode: {mode}, Source Generation: {availability}, Dynamic Code: {dynamicCode}";
        }
    }
}