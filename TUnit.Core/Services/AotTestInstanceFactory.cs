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
        throw new NotSupportedException(
            "AOT test instance creation should use source-generated factories. " +
            "This method should not be called in AOT scenarios.");
    }

    /// <inheritdoc />
    public Task<object?> InvokeMethodAsync(object instance, MethodInfo method, object?[] args)
    {
        throw new NotSupportedException(
            "AOT method invocation should use source-generated invokers. " +
            "This method should not be called in AOT scenarios.");
    }

    /// <inheritdoc />
    public Task SetPropertyAsync(object instance, PropertyInfo property, object? value)
    {
        throw new NotSupportedException(
            "AOT property setting should use source-generated setters. " +
            "This method should not be called in AOT scenarios.");
    }
}
