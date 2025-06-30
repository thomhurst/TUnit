using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;

namespace TUnit.Core.Services;

/// <summary>
/// AOT-only test variation executor using source generation exclusively.
/// This implementation eliminates all reflection usage for AOT compatibility.
/// </summary>
public class AotTestVariationExecutor : ITestVariationExecutor
{
    private readonly ISourceGeneratedTestRegistry _registry;

    public AotTestVariationExecutor(ISourceGeneratedTestRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public async Task<object> CreateTestInstanceAsync(TestVariation variation)
    {
        // Force source generation mode
        if (variation.ExecutionMode == TestExecutionMode.Reflection)
        {
            throw new NotSupportedException(
                "Reflection mode is not supported in AOT builds. Use source generation mode only.");
        }

        // Try to get factory from registry first
        var instance = TryCreateFromRegistry(variation);
        if (instance != null)
        {
            await SetPropertiesFromRegistry(variation, instance);
            return instance;
        }

        // Fall back to strongly-typed delegates
        var className = variation.ClassMetadata.Type.FullName ?? variation.ClassMetadata.Type.Name;
        
        // Try parameterized factory if we have class arguments
        if (variation.ClassArguments?.Length > 0)
        {
            var paramFactory = TestDelegateStorage.GetInstanceFactory($"{className}_ParameterizedFactory");
            if (paramFactory != null)
            {
                instance = paramFactory(variation.ClassArguments);
                await SetPropertiesFromDelegates(variation, instance);
                return instance;
            }
        }

        // Try simple factory
        var factory = TestDelegateStorage.GetInstanceFactory($"{className}_Factory");
        if (factory != null)
        {
            instance = factory([]);
            await SetPropertiesFromDelegates(variation, instance);
            return instance;
        }

        throw new InvalidOperationException(
            $"No factory found for test {variation.TestId} in AOT mode. Ensure source generators have run.");
    }

    /// <inheritdoc />
    public async Task<object?> InvokeTestMethodAsync(TestVariation variation, object instance)
    {
        if (variation.ExecutionMode == TestExecutionMode.Reflection)
        {
            throw new NotSupportedException(
                "Reflection mode is not supported in AOT builds. Use source generation mode only.");
        }

        // Try strongly-typed delegate first
        var stronglyTypedDelegate = TestDelegateStorage.GetStronglyTypedDelegate(variation.TestId);
        if (stronglyTypedDelegate != null)
        {
            var args = variation.MethodArguments ?? [];
            var result = stronglyTypedDelegate.DynamicInvoke([instance, ..args]);
            
            if (result is Task task)
            {
                await task;
                return null;
            }
            
            return result;
        }

        // Try registry invoker
        var invoker = _registry.GetMethodInvoker(variation.TestId);
        if (invoker != null)
        {
            var args = variation.MethodArguments ?? [];
            return await invoker(instance, args);
        }

        // Try generic invoker
        var genericInvoker = TestDelegateStorage.GetTestInvoker(variation.TestId);
        if (genericInvoker != null)
        {
            var args = variation.MethodArguments ?? [];
            await genericInvoker(instance, args);
            return null;
        }

        throw new InvalidOperationException(
            $"No method invoker found for test {variation.TestId} in AOT mode. Ensure source generators have run.");
    }

    /// <inheritdoc />
    public async Task SetPropertiesAsync(TestVariation variation, object instance)
    {
        if (variation.ExecutionMode == TestExecutionMode.Reflection)
        {
            throw new NotSupportedException(
                "Reflection mode is not supported in AOT builds. Use source generation mode only.");
        }

        await SetPropertiesFromRegistry(variation, instance);
        await SetPropertiesFromDelegates(variation, instance);
    }

    /// <inheritdoc />
    public bool SupportsVariation(TestVariation variation)
    {
        // Only support source generation mode in AOT
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

    private async Task SetPropertiesFromDelegates(TestVariation variation, object instance)
    {
        if (variation.PropertyValues == null || variation.PropertyValues.Count == 0)
        {
            return;
        }

        var className = variation.ClassMetadata.Type.FullName ?? variation.ClassMetadata.Type.Name;
        var bulkSetter = TestDelegateStorage.GetBulkPropertySetter(className);
        
        if (bulkSetter != null)
        {
            // Use service provider if available
            var serviceProvider = variation.PropertyValues.ContainsKey("ServiceProvider") 
                ? variation.PropertyValues["ServiceProvider"] as IServiceProvider
                : null;
                
            if (serviceProvider != null)
            {
                bulkSetter(instance, serviceProvider);
                return;
            }
        }

        // Fall back to individual property setters
        foreach (var (propertyName, propertyValue) in variation.PropertyValues)
        {
            var propertyKey = $"{className}.{propertyName}";
            var setter = TestDelegateStorage.GetPropertySetter(propertyKey);
            if (setter != null)
            {
                setter(instance, propertyValue);
            }
        }

        await Task.CompletedTask;
    }
}
