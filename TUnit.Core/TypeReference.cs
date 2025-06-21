using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Represents a type reference that can be resolved at runtime.
/// Used by source generators to represent types that may include generic parameters.
/// </summary>
public sealed class TypeReference
{
    /// <summary>
    /// For concrete types and generic type definitions.
    /// Example: "System.String, System.Private.CoreLib" or "System.Collections.Generic.List`1, System.Private.CoreLib"
    /// </summary>
    public string? AssemblyQualifiedName { get; set; }

    /// <summary>
    /// True if this represents a generic parameter (e.g., T in MyClass&lt;T&gt;).
    /// </summary>
    public bool IsGenericParameter { get; set; }

    /// <summary>
    /// The 0-based position of the generic parameter.
    /// For MyClass&lt;T, U&gt;, T has position 0, U has position 1.
    /// </summary>
    public int GenericParameterPosition { get; set; }

    /// <summary>
    /// True if this is a method generic parameter, false if it's a type generic parameter.
    /// </summary>
    public bool IsMethodGenericParameter { get; set; }

    /// <summary>
    /// The name of the generic parameter (e.g., "T", "TKey").
    /// Used for debugging and error messages.
    /// </summary>
    public string? GenericParameterName { get; set; }

    /// <summary>
    /// For constructed generic types (e.g., List&lt;int&gt;, Dictionary&lt;string, T&gt;).
    /// Contains TypeReference instances for the generic arguments.
    /// </summary>
    public List<TypeReference> GenericArguments { get; set; } = new();

    /// <summary>
    /// True if this represents an array type.
    /// </summary>
    public bool IsArray { get; set; }

    /// <summary>
    /// The element type for arrays (e.g., int for int[], T for T[]).
    /// </summary>
    public TypeReference? ElementType { get; set; }

    /// <summary>
    /// The rank of the array. 1 for [], 2 for [,], etc.
    /// </summary>
    public int ArrayRank { get; set; } = 1;

    /// <summary>
    /// True if this represents a pointer type (e.g., int*).
    /// </summary>
    public bool IsPointer { get; set; }

    /// <summary>
    /// True if this represents a by-reference type (e.g., ref int).
    /// </summary>
    public bool IsByRef { get; set; }

    /// <summary>
    /// Creates a TypeReference for a concrete type.
    /// </summary>
    public static TypeReference CreateConcrete(string assemblyQualifiedName)
    {
        return new TypeReference
        {
            AssemblyQualifiedName = assemblyQualifiedName,
            IsGenericParameter = false
        };
    }

    /// <summary>
    /// Creates a TypeReference for a generic parameter.
    /// </summary>
    public static TypeReference CreateGenericParameter(int position, bool isMethodParameter, string? name = null)
    {
        return new TypeReference
        {
            IsGenericParameter = true,
            GenericParameterPosition = position,
            IsMethodGenericParameter = isMethodParameter,
            GenericParameterName = name
        };
    }

    /// <summary>
    /// Creates a TypeReference for an array type.
    /// </summary>
    public static TypeReference CreateArray(TypeReference elementType, int rank = 1)
    {
        return new TypeReference
        {
            IsArray = true,
            ElementType = elementType,
            ArrayRank = rank
        };
    }

    /// <summary>
    /// Creates a TypeReference for a constructed generic type.
    /// </summary>
    public static TypeReference CreateConstructedGeneric(string genericTypeDefinition, params TypeReference[] genericArguments)
    {
        var typeRef = new TypeReference
        {
            AssemblyQualifiedName = genericTypeDefinition
        };
        typeRef.GenericArguments.AddRange(genericArguments);
        return typeRef;
    }
}