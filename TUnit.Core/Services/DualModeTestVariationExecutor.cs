using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;

namespace TUnit.Core.Services;

/// <summary>
/// Dual-mode test variation executor that routes to appropriate implementation based on execution mode.
/// This provides a unified API while supporting both source generation and reflection modes.
/// </summary>
public class DualModeTestVariationExecutor : ITestVariationExecutor
{
    private readonly SourceGenerationTestVariationExecutor _sourceGenerationExecutor;
    private readonly ReflectionTestVariationExecutor _reflectionExecutor;

    public DualModeTestVariationExecutor(
        SourceGenerationTestVariationExecutor sourceGenerationExecutor,
        ReflectionTestVariationExecutor reflectionExecutor)
    {
        _sourceGenerationExecutor = sourceGenerationExecutor;
        _reflectionExecutor = reflectionExecutor;
    }

    /// <inheritdoc />
    public Task<object> CreateTestInstanceAsync(TestVariation variation)
    {
        var executor = GetExecutor(variation);
        return executor.CreateTestInstanceAsync(variation);
    }

    /// <inheritdoc />
    public Task<object?> InvokeTestMethodAsync(TestVariation variation, object instance)
    {
        var executor = GetExecutor(variation);
        return executor.InvokeTestMethodAsync(variation, instance);
    }

    /// <inheritdoc />
    public Task SetPropertiesAsync(TestVariation variation, object instance)
    {
        var executor = GetExecutor(variation);
        return executor.SetPropertiesAsync(variation, instance);
    }

    /// <inheritdoc />
    public bool SupportsVariation(TestVariation variation)
    {
        var executor = GetExecutor(variation);
        return executor.SupportsVariation(variation);
    }

    /// <summary>
    /// Gets the appropriate executor for the specified variation.
    /// </summary>
    private ITestVariationExecutor GetExecutor(TestVariation variation)
    {
        return variation.ExecutionMode switch
        {
            TestExecutionMode.SourceGeneration => _sourceGenerationExecutor,
            TestExecutionMode.Reflection => _reflectionExecutor,
            _ => throw new NotSupportedException($"Execution mode {variation.ExecutionMode} is not supported")
        };
    }
}
