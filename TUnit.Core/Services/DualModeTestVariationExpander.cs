using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;

namespace TUnit.Core.Services;

/// <summary>
/// Dual-mode test variation expander that routes to appropriate implementation based on execution mode.
/// This provides a unified API while supporting both source generation and reflection modes.
/// </summary>
public class DualModeTestVariationExpander : ITestVariationExpander
{
    private readonly IModeDetector _modeDetector;
    private readonly ITestVariationExpander _sourceGenerationExpander;
    private readonly ITestVariationExpander _reflectionExpander;
    private readonly TestExecutionMode _detectedMode;

    public DualModeTestVariationExpander(
        IModeDetector modeDetector,
        SourceGenerationTestVariationExpander sourceGenerationExpander,
        ReflectionTestVariationExpander reflectionExpander)
    {
        _modeDetector = modeDetector;
        _sourceGenerationExpander = sourceGenerationExpander;
        _reflectionExpander = reflectionExpander;
        _detectedMode = modeDetector.DetectMode();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TestVariation> ExpandTestVariationsAsync(
        ITestDescriptor testDescriptor, 
        CancellationToken cancellationToken = default)
    {
        var expander = GetExpander(_detectedMode);
        return expander.ExpandTestVariationsAsync(testDescriptor, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> EstimateVariationCountAsync(ITestDescriptor testDescriptor)
    {
        var expander = GetExpander(_detectedMode);
        return expander.EstimateVariationCountAsync(testDescriptor);
    }

    /// <summary>
    /// Gets the appropriate expander for the specified mode.
    /// </summary>
    private ITestVariationExpander GetExpander(TestExecutionMode mode)
    {
        return mode switch
        {
            TestExecutionMode.SourceGeneration => _sourceGenerationExpander,
            TestExecutionMode.Reflection => _reflectionExpander,
            _ => throw new NotSupportedException($"Execution mode {mode} is not supported")
        };
    }

    /// <summary>
    /// Gets the current execution mode.
    /// </summary>
    public TestExecutionMode CurrentMode => _detectedMode;
}