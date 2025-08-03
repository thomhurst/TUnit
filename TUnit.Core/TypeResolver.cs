using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

/// <summary>
/// AOT-compatible type resolver that works with source-generated type mappings.
/// This version eliminates all dynamic type creation and runtime type resolution.
/// </summary>
public sealed class TypeResolver
{
    private readonly ConcurrentDictionary<string, Type> _typeCache = new();
    private readonly Dictionary<(int position, bool isMethodParameter), Type> _genericParameterMap;
    private readonly Type? _declaringType;
    private readonly MethodInfo? _declaringMethod;

    public TypeResolver(Type? declaringType = null, MethodInfo? declaringMethod = null)
    {
        _declaringType = declaringType;
        _declaringMethod = declaringMethod;
        _genericParameterMap = BuildGenericParameterMap(declaringType, declaringMethod);
    }

    public static TypeResolver FromTestInstance(object testInstance, MethodInfo? testMethod = null)
    {
        var testType = testInstance.GetType();
        return new TypeResolver(testType, testMethod);
    }

    /// <summary>
    /// Resolves TypeReference objects using AOT-safe mechanisms.
    /// This method requires source-generated type mappings to work correctly in AOT scenarios.
    /// </summary>
    public Type Resolve(TypeReference typeReference)
    {
        if (typeReference == null)
        {
            throw new ArgumentNullException(nameof(typeReference));
        }

        // Handle generic parameters using the pre-built map
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

        // Handle array types by delegating to element type resolution
        if (typeReference.IsArray)
        {
            if (typeReference.ElementType == null)
            {
                throw new InvalidOperationException("Array TypeReference must have ElementType");
            }

            var elementType = Resolve(typeReference.ElementType);
            return CreateArrayType(elementType, typeReference.ArrayRank);
        }

        // Handle pointer types
        if (typeReference.IsPointer)
        {
            if (typeReference.ElementType == null)
            {
                throw new InvalidOperationException("Pointer TypeReference must have ElementType");
            }

            var elementType = Resolve(typeReference.ElementType);
            return CreatePointerType(elementType);
        }

        // Handle ByRef types
        if (typeReference.IsByRef)
        {
            if (typeReference.ElementType == null)
            {
                throw new InvalidOperationException("ByRef TypeReference must have ElementType");
            }

            var elementType = Resolve(typeReference.ElementType);
            return CreateByRefType(elementType);
        }

        // Handle concrete types
        if (string.IsNullOrEmpty(typeReference.AssemblyQualifiedName))
        {
            throw new InvalidOperationException("Non-generic TypeReference must have AssemblyQualifiedName");
        }

        // Check cache first
        if (_typeCache.TryGetValue(typeReference.AssemblyQualifiedName!, out var cachedType))
        {
            return ApplyGenericArguments(cachedType, typeReference);
        }

        // In AOT mode, we cannot use Type.GetType() safely
        // Types must be provided through source-generated registrations
        var type = TypeRegistry.GetRegisteredType(typeReference.AssemblyQualifiedName!);
        if (type == null)
        {
            throw new InvalidOperationException(
                $"Type not registered for AOT compilation: {typeReference.AssemblyQualifiedName}. " +
                "Ensure type is registered in source-generated TypeRegistry.");
        }

        _typeCache[typeReference.AssemblyQualifiedName!] = type;
        return ApplyGenericArguments(type, typeReference);
    }

    public Type[] ResolveMany(IEnumerable<TypeReference> typeReferences)
    {
        return typeReferences.Select(Resolve).ToArray();
    }

    /// <summary>
    /// Applies generic arguments to a type using AOT-safe mechanisms.
    /// This method requires source-generated generic type mappings.
    /// </summary>
    private Type ApplyGenericArguments(Type type, TypeReference typeReference)
    {
        if (typeReference.GenericArguments.Count == 0)
        {
            return type;
        }

        var genericArgs = typeReference.GenericArguments
            .Select(Resolve)
            .ToArray();

        if (type.IsGenericTypeDefinition)
        {
            // In AOT mode, we need to use pre-constructed generic types
            // rather than MakeGenericType which is not AOT-compatible
            var constructedType = TypeRegistry.GetConstructedGenericType(type, genericArgs);
            if (constructedType != null)
            {
                return constructedType;
            }

            throw new InvalidOperationException(
                $"Constructed generic type not registered for AOT compilation: {type.Name}<{string.Join(",", genericArgs.Select(t => t.Name))}>. " +
                "Ensure all generic type combinations are registered in source-generated TypeRegistry.");
        }

        // If it's already a constructed generic type, return as-is
        if (type.IsConstructedGenericType)
        {
            return type;
        }

        return type;
    }

    /// <summary>
    /// Creates array types using AOT-safe mechanisms.
    /// </summary>
    private static Type CreateArrayType(Type elementType, int rank)
    {
        // For AOT compatibility, we need to use pre-registered array types
        var arrayType = TypeRegistry.GetArrayType(elementType, rank);
        if (arrayType != null)
        {
            return arrayType;
        }

        throw new InvalidOperationException(
            $"Array type not registered for AOT compilation: {elementType.Name}[{new string(',', rank - 1)}]. " +
            "Ensure array type is registered in source-generated TypeRegistry.");
    }

    /// <summary>
    /// Creates pointer types using AOT-safe mechanisms.
    /// </summary>
    private static Type CreatePointerType(Type elementType)
    {
        var pointerType = TypeRegistry.GetPointerType(elementType);
        if (pointerType != null)
        {
            return pointerType;
        }

        throw new InvalidOperationException(
            $"Pointer type not registered for AOT compilation: {elementType.Name}*. " +
            "Ensure pointer type is registered in source-generated TypeRegistry.");
    }

    /// <summary>
    /// Creates ByRef types using AOT-safe mechanisms.
    /// </summary>
    private static Type CreateByRefType(Type elementType)
    {
        var byRefType = TypeRegistry.GetByRefType(elementType);
        if (byRefType != null)
        {
            return byRefType;
        }

        throw new InvalidOperationException(
            $"ByRef type not registered for AOT compilation: {elementType.Name}&. " +
            "Ensure ByRef type is registered in source-generated TypeRegistry.");
    }

    private static Dictionary<(int position, bool isMethodParameter), Type> BuildGenericParameterMap(
        Type? declaringType,
        MethodInfo? declaringMethod)
    {
        var map = new Dictionary<(int position, bool isMethodParameter), Type>();

        if (declaringType?.IsGenericType == true)
        {
            var genericArgs = declaringType.GetGenericArguments();
            for (var i = 0; i < genericArgs.Length; i++)
            {
                map[(i, false)] = genericArgs[i];
            }
        }

        if (declaringMethod?.IsGenericMethodDefinition == true)
        {
            var genericArgs = declaringMethod.GetGenericArguments();
            for (var i = 0; i < genericArgs.Length; i++)
            {
                map[(i, true)] = genericArgs[i];
            }
        }

        return map;
    }

    public static TypeResolver CreateSimple()
    {
        return new TypeResolver();
    }
}

/// <summary>
/// AOT-compatible type registry that provides pre-registered types.
/// This class is populated by source generators at compile time.
/// </summary>
public static class TypeRegistry
{
    private static readonly ConcurrentDictionary<string, Type> _registeredTypes = new();
    private static readonly ConcurrentDictionary<(Type genericDefinition, Type[] arguments), Type> _constructedGenericTypes = new();
    private static readonly ConcurrentDictionary<(Type elementType, int rank), Type> _arrayTypes = new();
    private static readonly ConcurrentDictionary<Type, Type> _pointerTypes = new();
    private static readonly ConcurrentDictionary<Type, Type> _byRefTypes = new();

    /// <summary>
    /// Registers a type for AOT-safe resolution.
    /// This method is called by source generators.
    /// </summary>
    public static void RegisterType(string assemblyQualifiedName, Type type)
    {
        _registeredTypes.TryAdd(assemblyQualifiedName, type);
    }

    /// <summary>
    /// Registers a constructed generic type for AOT-safe resolution.
    /// This method is called by source generators.
    /// </summary>
    public static void RegisterConstructedGenericType(Type genericDefinition, Type[] genericArguments, Type constructedType)
    {
        _constructedGenericTypes.TryAdd((genericDefinition, genericArguments), constructedType);
    }

    /// <summary>
    /// Registers an array type for AOT-safe resolution.
    /// This method is called by source generators.
    /// </summary>
    public static void RegisterArrayType(Type elementType, int rank, Type arrayType)
    {
        _arrayTypes.TryAdd((elementType, rank), arrayType);
    }

    /// <summary>
    /// Registers a pointer type for AOT-safe resolution.
    /// This method is called by source generators.
    /// </summary>
    public static void RegisterPointerType(Type elementType, Type pointerType)
    {
        _pointerTypes.TryAdd(elementType, pointerType);
    }

    /// <summary>
    /// Registers a ByRef type for AOT-safe resolution.
    /// This method is called by source generators.
    /// </summary>
    public static void RegisterByRefType(Type elementType, Type byRefType)
    {
        _byRefTypes.TryAdd(elementType, byRefType);
    }

    /// <summary>
    /// Gets a registered type by its assembly qualified name.
    /// </summary>
    internal static Type? GetRegisteredType(string assemblyQualifiedName)
    {
        return _registeredTypes.TryGetValue(assemblyQualifiedName, out var type) ? type : null;
    }

    /// <summary>
    /// Gets a constructed generic type for the given definition and arguments.
    /// </summary>
    internal static Type? GetConstructedGenericType(Type genericDefinition, Type[] genericArguments)
    {
        return _constructedGenericTypes.TryGetValue((genericDefinition, genericArguments), out var type) ? type : null;
    }

    /// <summary>
    /// Gets an array type for the given element type and rank.
    /// </summary>
    internal static Type? GetArrayType(Type elementType, int rank)
    {
        return _arrayTypes.TryGetValue((elementType, rank), out var type) ? type : null;
    }

    /// <summary>
    /// Gets a pointer type for the given element type.
    /// </summary>
    internal static Type? GetPointerType(Type elementType)
    {
        return _pointerTypes.TryGetValue(elementType, out var type) ? type : null;
    }

    /// <summary>
    /// Gets a ByRef type for the given element type.
    /// </summary>
    internal static Type? GetByRefType(Type elementType)
    {
        return _byRefTypes.TryGetValue(elementType, out var type) ? type : null;
    }

    /// <summary>
    /// Gets all registered types for diagnostic purposes.
    /// </summary>
    public static IReadOnlyDictionary<string, Type> GetAllRegisteredTypes()
    {
        return _registeredTypes;
    }
}