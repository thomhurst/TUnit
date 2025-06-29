using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// AOT-safe implementation of IGenericTypeResolver that doesn't support generic resolution
/// This is used when source generation handles all generic resolution at compile time
/// </summary>
public class NoOpGenericTypeResolver : IGenericTypeResolver
{
    /// <inheritdoc />
    public Type[] ResolveGenericMethodArguments(MethodInfo genericMethodDefinition, object?[] runtimeArguments)
    {
        throw new NotSupportedException(
            "Generic type resolution is not supported in AOT mode. " +
            "All generic test methods should be resolved at compile time through source generation.");
    }

    /// <inheritdoc />
    public Type[] ResolveGenericClassArguments(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type genericTypeDefinition,
        object?[] constructorArguments)
    {
        throw new NotSupportedException(
            "Generic type resolution is not supported in AOT mode. " +
            "All generic test classes should be resolved at compile time through source generation.");
    }
}
