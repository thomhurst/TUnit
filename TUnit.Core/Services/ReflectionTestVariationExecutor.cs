using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;

namespace TUnit.Core.Services;

/// <summary>
/// Reflection mode executor that executes test variations using runtime reflection.
/// This implementation provides full flexibility but requires dynamic code generation capabilities.
/// </summary>
[RequiresDynamicCode("Reflection mode test execution requires runtime type generation")]
[RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
public class ReflectionTestVariationExecutor : ITestVariationExecutor
{
    private readonly ITestInstanceFactory _testInstanceFactory;

    public ReflectionTestVariationExecutor(ITestInstanceFactory testInstanceFactory)
    {
        _testInstanceFactory = testInstanceFactory;
    }

    /// <inheritdoc />
    public async Task<object> CreateTestInstanceAsync(TestVariation variation)
    {
        if (!SupportsVariation(variation))
        {
            throw new NotSupportedException(
                $"Reflection executor does not support {variation.ExecutionMode} mode");
        }

        var classType = variation.ClassMetadata.Type;
        var classArgs = variation.ClassArguments ?? Array.Empty<object?>();

        // Use the test instance factory to create the instance via reflection
        var instance = await _testInstanceFactory.CreateInstanceAsync(classType, classArgs);

        // Set properties via reflection
        await SetPropertiesAsync(variation, instance);

        return instance;
    }

    /// <inheritdoc />
    public async Task<object?> InvokeTestMethodAsync(TestVariation variation, object instance)
    {
        if (!SupportsVariation(variation))
        {
            throw new NotSupportedException(
                $"Reflection executor does not support {variation.ExecutionMode} mode");
        }

        // Get the method info from metadata
        var methodInfo = GetMethodInfo(variation.MethodMetadata);
        var methodArgs = variation.MethodArguments ?? Array.Empty<object?>();

        // Use the test instance factory to invoke the method via reflection
        return await _testInstanceFactory.InvokeMethodAsync(instance, methodInfo, methodArgs);
    }

    /// <inheritdoc />
    public async Task SetPropertiesAsync(TestVariation variation, object instance)
    {
        if (!SupportsVariation(variation))
        {
            throw new NotSupportedException(
                $"Reflection executor does not support {variation.ExecutionMode} mode");
        }

        if (variation.PropertyValues == null)
        {
            return;
        }

        // Set each property via reflection
        foreach (var (propertyName, propertyValue) in variation.PropertyValues)
        {
            var propertyInfo = GetPropertyInfo(variation.ClassMetadata.Type, propertyName);
            if (propertyInfo != null)
            {
                await _testInstanceFactory.SetPropertyAsync(instance, propertyInfo, propertyValue);
            }
        }
    }

    /// <inheritdoc />
    public bool SupportsVariation(TestVariation variation)
    {
        return variation.ExecutionMode == TestExecutionMode.Reflection;
    }

    private static MethodInfo GetMethodInfo(MethodMetadata methodMetadata)
    {
        // Convert MethodMetadata back to MethodInfo
        // This is a simplified implementation - might need enhancement based on MethodMetadata structure
        var type = methodMetadata.DeclaringType();
        var method = type.GetMethod(methodMetadata.MethodName(), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        
        if (method == null)
        {
            throw new InvalidOperationException(
                $"Could not find method {methodMetadata.MethodName()} on type {type.FullName}");
        }

        return method;
    }

    private static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
    {
        return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }
}