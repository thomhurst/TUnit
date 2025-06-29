using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;

namespace TUnit.Core.Services;

/// <summary>
/// Source generation mode executor that executes test variations using pre-generated factories and invokers.
/// This implementation is 100% AOT-safe and avoids all reflection.
/// </summary>
public class SourceGenerationTestVariationExecutor : ITestVariationExecutor
{
    private readonly ISourceGeneratedTestRegistry _registry;

    public SourceGenerationTestVariationExecutor(ISourceGeneratedTestRegistry registry)
    {
        _registry = registry;
    }
    /// <inheritdoc />
    public async Task<object> CreateTestInstanceAsync(TestVariation variation)
    {
        if (!SupportsVariation(variation))
        {
            throw new NotSupportedException(
                $"Source generation executor does not support {variation.ExecutionMode} mode");
        }

        // Try to get factory from registry first
        var instance = TryCreateFromRegistry(variation);
        if (instance != null)
        {
            await SetPropertiesFromRegistry(variation, instance);
            return instance;
        }

        // Fall back to source generated data if available
        var sourceData = variation.SourceGeneratedData;
        if (sourceData?.ClassInstanceFactory != null)
        {
            instance = sourceData.ClassInstanceFactory();
            await SetPropertiesFromSourceData(variation, instance, sourceData);
            return instance;
        }

        throw new InvalidOperationException(
            $"No factory found for test {variation.TestId} in source generation mode");
    }

    /// <inheritdoc />
    public async Task<object?> InvokeTestMethodAsync(TestVariation variation, object instance)
    {
        if (!SupportsVariation(variation))
        {
            throw new NotSupportedException(
                $"Source generation executor does not support {variation.ExecutionMode} mode");
        }

        // Try to get invoker from registry first
        var invoker = _registry.GetMethodInvoker(variation.TestId);
        if (invoker != null)
        {
            var args = variation.MethodArguments ?? [];
            return await invoker(instance, args);
        }

        // Fall back to source generated data if available
        var sourceData = variation.SourceGeneratedData;
        if (sourceData?.MethodInvoker != null)
        {
            var args = variation.MethodArguments ?? [];
            return await sourceData.MethodInvoker(instance, args);
        }

        throw new InvalidOperationException(
            $"No method invoker found for test {variation.TestId} in source generation mode");
    }

    /// <inheritdoc />
    public async Task SetPropertiesAsync(TestVariation variation, object instance)
    {
        if (!SupportsVariation(variation))
        {
            throw new NotSupportedException(
                $"Source generation executor does not support {variation.ExecutionMode} mode");
        }

        await SetPropertiesFromRegistry(variation, instance);
    }

    /// <inheritdoc />
    public bool SupportsVariation(TestVariation variation)
    {
        return variation.ExecutionMode == TestExecutionMode.SourceGeneration;
    }

    private object? TryCreateFromRegistry(TestVariation variation)
    {
        // Try parameterized factory first if we have class arguments
        if (variation.ClassArguments?.Length > 0)
        {
            var parameterizedFactory = _registry.GetParameterizedClassFactory(variation.TestId);
            if (parameterizedFactory != null)
            {
                return parameterizedFactory(variation.ClassArguments);
            }
        }

        // Try simple factory
        var factory = _registry.GetClassFactory(variation.TestId);
        return factory?.Invoke();
    }

    private async Task SetPropertiesFromRegistry(TestVariation variation, object instance)
    {
        if (variation.PropertyValues == null || variation.PropertyValues.Count == 0)
        {
            return;
        }

        var propertySetters = _registry.GetPropertySetters(variation.TestId);

        foreach (var (propertyName, propertyValue) in variation.PropertyValues)
        {
            if (propertySetters.TryGetValue(propertyName, out var setter))
            {
                setter(instance, propertyValue);
            }
        }

        await Task.CompletedTask;
    }

    private static async Task SetPropertiesFromSourceData(
        TestVariation variation,
        object instance,
        SourceGeneratedTestData sourceData)
    {
        if (sourceData.PropertySetters == null || variation.PropertyValues == null)
        {
            return;
        }

        foreach (var (propertyName, propertyValue) in variation.PropertyValues)
        {
            if (sourceData.PropertySetters.TryGetValue(propertyName, out var setter))
            {
                setter(instance, propertyValue);
            }
        }

        await Task.CompletedTask;
    }
}
