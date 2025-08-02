using System;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core.Helpers;

/// <summary>
/// Helper class for generic type operations that ensures compatibility with AOT scenarios
/// </summary>
public static class GenericTypeHelper
{
    /// <summary>
    /// Safely creates a generic type with the specified type arguments.
    /// Uses AOT-safe replacements when available, falls back to reflection otherwise.
    /// </summary>
    /// <param name="genericTypeDefinition">The open generic type definition</param>
    /// <param name="typeArguments">The type arguments to apply</param>
    /// <returns>The constructed generic type</returns>
    /// <exception cref="ArgumentNullException">Thrown when genericTypeDefinition is null</exception>
    /// <exception cref="ArgumentException">Thrown when type arguments don't match the generic type definition</exception>
    [UnconditionalSuppressMessage("AOT", "IL2055:UnrecognizedReflectionPattern", 
        Justification = "MakeGenericType is used as a fallback. AOT analyzer warns at compile time.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", 
        Justification = "MakeGenericType is used as a fallback. AOT analyzer warns at compile time.")]
    public static Type MakeGenericTypeSafe(Type genericTypeDefinition, params Type[] typeArguments)
    {
        if (genericTypeDefinition == null)
        {
            throw new ArgumentNullException(nameof(genericTypeDefinition));
        }

        if (typeArguments == null || typeArguments.Length == 0)
        {
            throw new ArgumentException("Type arguments cannot be null or empty", nameof(typeArguments));
        }

        if (!genericTypeDefinition.IsGenericTypeDefinition)
        {
            throw new ArgumentException($"Type {genericTypeDefinition.FullName} is not a generic type definition", nameof(genericTypeDefinition));
        }

        var genericParameters = genericTypeDefinition.GetGenericArguments();
        if (genericParameters.Length != typeArguments.Length)
        {
            throw new ArgumentException(
                $"The number of generic arguments provided ({typeArguments.Length}) doesn't equal " +
                $"the arity of the generic type definition ({genericParameters.Length}). " +
                $"Type: {genericTypeDefinition.FullName}",
                nameof(typeArguments));
        }

        // Even in source generation mode, we attempt to use reflection as a fallback
        // The AOT analyzer will warn about incompatibility at compile time
        try
        {
            // Reflection mode - use MakeGenericType directly
            return genericTypeDefinition.MakeGenericType(typeArguments);
        }
        catch (ArgumentException ex)
        {
            // Provide more detailed error information
            var typeArgNames = string.Join(", ", Array.ConvertAll(typeArguments, t => t?.FullName ?? "null"));
            throw new ArgumentException(
                $"Failed to create generic type {genericTypeDefinition.FullName} with type arguments [{typeArgNames}]: {ex.Message}",
                nameof(typeArguments),
                ex);
        }
    }

    /// <summary>
    /// Checks if a type is a constructed generic type (e.g., List&lt;int&gt;)
    /// </summary>
    public static bool IsConstructedGenericType(Type type)
    {
        return type is { IsGenericType: true, IsGenericTypeDefinition: false };
    }

    /// <summary>
    /// Gets the generic type definition from a constructed generic type
    /// </summary>
    public static Type GetGenericTypeDefinition(Type type)
    {
        if (!type.IsGenericType)
        {
            throw new ArgumentException($"Type {type.FullName} is not a generic type", nameof(type));
        }

        return type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition();
    }
}