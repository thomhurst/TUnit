using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

/// <summary>
/// Resolves TypeReference objects to actual Type instances at runtime.
/// Handles generic parameter resolution, array types, and nested generics.
/// </summary>
[RequiresDynamicCode("TypeResolver uses dynamic type creation for generics, arrays, and other complex types")]
[RequiresUnreferencedCode("TypeResolver uses Type.GetType which may require types that aren't statically referenced")]
public sealed class TypeResolver
{
    private readonly ConcurrentDictionary<string, Type> _typeCache = new();
    private readonly Dictionary<(int position, bool isMethodParameter), Type> _genericParameterMap;
    private readonly Type? _declaringType;
    private readonly MethodInfo? _declaringMethod;

    /// <summary>
    /// Creates a TypeResolver for a specific generic context.
    /// </summary>
    /// <param name="declaringType">The type containing generic parameters (if any)</param>
    /// <param name="declaringMethod">The method containing generic parameters (if any)</param>
    public TypeResolver(Type? declaringType = null, MethodInfo? declaringMethod = null)
    {
        _declaringType = declaringType;
        _declaringMethod = declaringMethod;
        _genericParameterMap = BuildGenericParameterMap(declaringType, declaringMethod);
    }

    /// <summary>
    /// Creates a TypeResolver from a test instance, extracting generic arguments from its runtime type.
    /// </summary>
    public static TypeResolver FromTestInstance(object testInstance, MethodInfo? testMethod = null)
    {
        var testType = testInstance.GetType();
        return new TypeResolver(testType, testMethod);
    }

    /// <summary>
    /// Resolves a TypeReference to an actual Type.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2055:MakeGenericType", Justification = "TypeResolver is not AOT-compatible by design")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "TypeResolver is not AOT-compatible by design")]
    [UnconditionalSuppressMessage("AOT", "IL2057:TypeGetType", Justification = "TypeResolver is not AOT-compatible by design")]
    [UnconditionalSuppressMessage("AOT", "IL2073:ReturnValueMismatch", Justification = "TypeResolver is not AOT-compatible by design")]
    public Type Resolve(TypeReference typeReference)
    {
        if (typeReference == null)
        {
            throw new ArgumentNullException(nameof(typeReference));
        }

        // Handle generic parameters
        if (typeReference.IsGenericParameter)
        {
            var key = (typeReference.GenericParameterPosition, typeReference.IsMethodGenericParameter);
            if (_genericParameterMap.TryGetValue(key, out var resolvedType))
            {
                return resolvedType;
            }

            throw new InvalidOperationException(
                $"Cannot resolve generic parameter at position {typeReference.GenericParameterPosition} " +
                $"(isMethod: {typeReference.IsMethodGenericParameter}). " +
                $"Available parameters: {string.Join(", ", _genericParameterMap.Keys)}");
        }

        // Handle array types
        if (typeReference.IsArray)
        {
            if (typeReference.ElementType == null)
            {
                throw new InvalidOperationException("Array TypeReference must have ElementType");
            }

            var elementType = Resolve(typeReference.ElementType);
            return typeReference.ArrayRank == 1
                ? elementType.MakeArrayType()
                : elementType.MakeArrayType(typeReference.ArrayRank);
        }

        // Handle pointer types
        if (typeReference.IsPointer)
        {
            if (typeReference.ElementType == null)
            {
                throw new InvalidOperationException("Pointer TypeReference must have ElementType");
            }

            var elementType = Resolve(typeReference.ElementType);
            return elementType.MakePointerType();
        }

        // Handle by-reference types
        if (typeReference.IsByRef)
        {
            if (typeReference.ElementType == null)
            {
                throw new InvalidOperationException("ByRef TypeReference must have ElementType");
            }

            var elementType = Resolve(typeReference.ElementType);
            return elementType.MakeByRefType();
        }

        // Handle concrete types
        if (string.IsNullOrEmpty(typeReference.AssemblyQualifiedName))
        {
            throw new InvalidOperationException("Non-generic TypeReference must have AssemblyQualifiedName");
        }

        // Try to get from cache first
        if (_typeCache.TryGetValue(typeReference.AssemblyQualifiedName!, out var cachedType))
        {
            return ApplyGenericArguments(cachedType, typeReference);
        }

        // Resolve the type
        var type = Type.GetType(typeReference.AssemblyQualifiedName);
        if (type == null)
        {
            throw new InvalidOperationException($"Cannot resolve type: {typeReference.AssemblyQualifiedName}");
        }

        _typeCache[typeReference.AssemblyQualifiedName!] = type;
        return ApplyGenericArguments(type, typeReference);
    }

    /// <summary>
    /// Resolves multiple TypeReferences in a single operation.
    /// </summary>
    public Type[] ResolveMany(IEnumerable<TypeReference> typeReferences)
    {
        return typeReferences.Select(Resolve).ToArray();
    }

    private Type ApplyGenericArguments(Type type, TypeReference typeReference)
    {
        if (typeReference.GenericArguments.Count == 0)
        {
            return type;
        }

        // Resolve generic arguments
        var genericArgs = typeReference.GenericArguments
            .Select(Resolve)
            .ToArray();

        // If this is a generic type definition, make it concrete
        if (type.IsGenericTypeDefinition)
        {
            return type.MakeGenericType(genericArgs);
        }

        // If it's already a constructed generic type, we might need to substitute arguments
        if (type.IsConstructedGenericType)
        {
            // This case happens when we have partially constructed generics
            // For now, return as-is, but this might need more sophisticated handling
            return type;
        }

        return type;
    }

    private static Dictionary<(int position, bool isMethodParameter), Type> BuildGenericParameterMap(
        Type? declaringType,
        MethodInfo? declaringMethod)
    {
        var map = new Dictionary<(int position, bool isMethodParameter), Type>();

        // Add type generic parameters
        if (declaringType?.IsGenericType == true)
        {
            var genericArgs = declaringType.GetGenericArguments();
            for (int i = 0; i < genericArgs.Length; i++)
            {
                map[(i, false)] = genericArgs[i];
            }
        }

        // Add method generic parameters
        if (declaringMethod?.IsGenericMethodDefinition == true)
        {
            var genericArgs = declaringMethod.GetGenericArguments();
            for (int i = 0; i < genericArgs.Length; i++)
            {
                map[(i, true)] = genericArgs[i];
            }
        }

        return map;
    }

    /// <summary>
    /// Creates a simple resolver that doesn't handle generic parameters.
    /// Useful for non-generic contexts.
    /// </summary>
    public static TypeResolver CreateSimple()
    {
        return new TypeResolver();
    }
}
