using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// AOT-safe implementation of test instance factory.
/// This implementation avoids reflection and works with source-generated metadata.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("Trimming", "IL2067")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
public class AotTestInstanceFactory : ITestInstanceFactory
{
    /// <inheritdoc />
    public Task<object> CreateInstanceAsync(Type type, object?[] args)
    {
        // In AOT mode with source generation, instances should be created through
        // the pre-generated factories rather than reflection
        throw new NotSupportedException(
            "AOT test instance creation should use source-generated factories. " +
            "This method should not be called in AOT scenarios.");
    }

    /// <inheritdoc />
    public Task<object?> InvokeMethodAsync(object instance, MethodInfo method, object?[] args)
    {
        // In AOT mode with source generation, method invocation should happen through
        // the pre-generated invokers rather than reflection
        throw new NotSupportedException(
            "AOT method invocation should use source-generated invokers. " +
            "This method should not be called in AOT scenarios.");
    }

    /// <inheritdoc />
    public Task SetPropertyAsync(object instance, PropertyInfo property, object? value)
    {
        // In AOT mode with source generation, property setting should happen through
        // the pre-generated setters rather than reflection
        throw new NotSupportedException(
            "AOT property setting should use source-generated setters. " +
            "This method should not be called in AOT scenarios.");
    }
}