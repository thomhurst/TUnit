using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Building;

namespace TUnit.Engine.Services;

/// <summary>
/// Resolves generic type arguments for tests based on TestMetadata and runtime TestData.
/// This class bridges the gap between compile-time generic information and runtime type resolution.
/// </summary>
internal sealed class TestGenericTypeResolver
{
    /// <summary>
    /// Resolves generic type arguments for a test class and/or method based on the provided test data.
    /// </summary>
    /// <param name="metadata">The test metadata containing generic type information</param>
    /// <param name="testData">The runtime test data containing actual arguments</param>
    /// <returns>A result containing resolved generic types for both class and method</returns>
    public static TestGenericTypeResolution Resolve(TestMetadata metadata, TestBuilder.TestData testData)
    {
        var result = new TestGenericTypeResolution();

        // Resolve class generic arguments if the test class is generic
        if (metadata.GenericTypeInfo != null)
        {
            result.ResolvedClassGenericArguments = ResolveClassGenericArguments(
                metadata.TestClassType,
                metadata.GenericTypeInfo,
                testData.ClassData);
        }

        // Resolve method generic arguments if the test method is generic
        if (metadata.GenericMethodInfo != null)
        {
            result.ResolvedMethodGenericArguments = ResolveMethodGenericArguments(
                metadata.MethodMetadata,
                metadata.GenericMethodInfo,
                testData.MethodData,
                metadata.ParameterTypes);
        }

        return result;
    }

    private static Type[] ResolveClassGenericArguments(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type genericClassType,
        GenericTypeInfo genericTypeInfo,
        object?[] constructorArguments)
    {
        if (!genericClassType.IsGenericTypeDefinition)
        {
            // If it's already constructed, extract the generic arguments
            if (genericClassType.IsConstructedGenericType)
            {
                return genericClassType.GetGenericArguments();
            }
            return Type.EmptyTypes;
        }

        var genericParameters = genericClassType.GetGenericArguments();
        var typeMapping = new Dictionary<Type, Type>();

        // Try to infer from constructor parameters
        var constructors = genericClassType.GetConstructors();
        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length == constructorArguments.Length)
            {
                typeMapping.Clear();
                if (TryInferTypesFromArguments(parameters, constructorArguments, typeMapping))
                {
                    break;
                }
            }
        }

        // Resolve all generic parameters
        var resolvedTypes = new Type[genericParameters.Length];
        for (var i = 0; i < genericParameters.Length; i++)
        {
            var genericParam = genericParameters[i];
            if (!typeMapping.TryGetValue(genericParam, out var resolvedType))
            {
                throw new GenericTypeResolutionException(
                    $"Could not resolve type for generic parameter '{genericParam.Name}' of type '{genericClassType.Name}'");
            }
            resolvedTypes[i] = resolvedType;
        }

        // Validate against constraints from metadata
        ValidateAgainstConstraints(genericTypeInfo.Constraints, resolvedTypes);

        return resolvedTypes;
    }

    private static Type[] ResolveMethodGenericArguments(
        MethodMetadata methodMetadata,
        GenericMethodInfo genericMethodInfo,
        object?[] methodArguments,
        Type[] parameterTypes)
    {

        var typeMapping = new Dictionary<Type, Type>();

        // Map parameter types to argument types
        for (var i = 0; i < Math.Min(parameterTypes.Length, methodArguments.Length); i++)
        {
            var paramType = parameterTypes[i];
            var argValue = methodArguments[i];

            if (argValue != null)
            {
                var argType = argValue.GetType();
                InferTypeMapping(paramType, argType, typeMapping, genericMethodInfo.ParameterPositions);
            }
        }

        // Create resolved types array based on the generic method info
        var resolvedTypes = new Type[genericMethodInfo.ParameterNames.Length];
        for (var i = 0; i < genericMethodInfo.ParameterNames.Length; i++)
        {
            // Try to find the resolved type in our mapping
            Type? resolvedType = null;
            foreach (var kvp in typeMapping)
            {
                if (kvp.Key.Name == genericMethodInfo.ParameterNames[i])
                {
                    resolvedType = kvp.Value;
                    break;
                }
            }

            if (resolvedType == null)
            {
                throw new GenericTypeResolutionException(
                    $"Could not resolve type for generic parameter '{genericMethodInfo.ParameterNames[i]}' in method");
            }

            resolvedTypes[i] = resolvedType;
        }

        // Validate against constraints from metadata
        ValidateAgainstConstraints(genericMethodInfo.Constraints, resolvedTypes);

        return resolvedTypes;
    }

    private static bool TryInferTypesFromArguments(
        ParameterInfo[] parameters,
        object?[] arguments,
        Dictionary<Type, Type> typeMapping)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            var argValue = arguments[i];

            if (argValue != null)
            {
                var argType = argValue.GetType();
                if (!TryInferTypeMapping(paramType, argType, typeMapping))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void InferTypeMapping(
        Type parameterType,
        Type argumentType,
        Dictionary<Type, Type> typeMapping,
        int[] genericParameterPositions)
    {
        if (!TryInferTypeMapping(parameterType, argumentType, typeMapping))
        {
            throw new GenericTypeResolutionException(
                $"Type mismatch: Cannot use argument of type '{argumentType.Name}' for parameter of type '{parameterType.Name}'");
        }
    }

    private static bool TryInferTypeMapping(Type parameterType, Type argumentType, Dictionary<Type, Type> typeMapping)
    {
        // Direct generic parameter
        if (parameterType.IsGenericParameter)
        {
            if (typeMapping.TryGetValue(parameterType, out var existingMapping))
            {
                // Verify consistency
                if (existingMapping != argumentType)
                {
                    return false;
                }
            }
            else
            {
                typeMapping[parameterType] = argumentType;
            }
            return true;
        }

        // Array types
        if (parameterType.IsArray && argumentType.IsArray)
        {
            return TryInferTypeMapping(
                parameterType.GetElementType()!,
                argumentType.GetElementType()!,
                typeMapping);
        }

        // Generic types (e.g., List<T>, Dictionary<K,V>)
        if (parameterType.IsGenericType && argumentType.IsGenericType)
        {
            var paramGenericDef = parameterType.GetGenericTypeDefinition();
            var argGenericDef = argumentType.IsGenericTypeDefinition 
                ? argumentType 
                : argumentType.GetGenericTypeDefinition();

            if (paramGenericDef == argGenericDef)
            {
                var paramGenericArgs = parameterType.GetGenericArguments();
                var argGenericArgs = argumentType.GetGenericArguments();

                if (paramGenericArgs.Length == argGenericArgs.Length)
                {
                    for (var i = 0; i < paramGenericArgs.Length; i++)
                    {
                        if (!TryInferTypeMapping(paramGenericArgs[i], argGenericArgs[i], typeMapping))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
        }

        // Non-generic types must match exactly or be assignable
        return parameterType.IsAssignableFrom(argumentType);
    }

    private static void ValidateAgainstConstraints(
        GenericParameterConstraints[] constraints,
        Type[] resolvedTypes)
    {
        for (var i = 0; i < Math.Min(constraints.Length, resolvedTypes.Length); i++)
        {
            var constraint = constraints[i];
            var resolvedType = resolvedTypes[i];

            // Check value type constraint
            if (constraint.HasValueTypeConstraint && !resolvedType.IsValueType)
            {
                throw new GenericTypeResolutionException(
                    $"Type '{resolvedType.Name}' does not satisfy the 'struct' constraint for generic parameter '{constraint.ParameterName}'");
            }

            // Check reference type constraint
            if (constraint.HasReferenceTypeConstraint && resolvedType.IsValueType)
            {
                throw new GenericTypeResolutionException(
                    $"Type '{resolvedType.Name}' does not satisfy the 'class' constraint for generic parameter '{constraint.ParameterName}'");
            }

            // Check default constructor constraint
            // Note: For AOT compatibility, we cannot reliably check for default constructors at runtime
            // The source generator will validate this at compile time instead
            if (constraint.HasDefaultConstructorConstraint && !resolvedType.IsValueType)
            {
                // In AOT mode, constructor constraints are validated at compile time
                // We skip runtime validation to avoid trimming issues
            }

            // Check base type constraint
            if (constraint.BaseTypeConstraint != null &&
                !constraint.BaseTypeConstraint.IsAssignableFrom(resolvedType))
            {
                throw new GenericTypeResolutionException(
                    $"Type '{resolvedType.Name}' does not satisfy the base type constraint '{constraint.BaseTypeConstraint.Name}' for generic parameter '{constraint.ParameterName}'");
            }

            // Check interface constraints
            foreach (var interfaceConstraint in constraint.InterfaceConstraints)
            {
                if (!interfaceConstraint.IsAssignableFrom(resolvedType))
                {
                    throw new GenericTypeResolutionException(
                        $"Type '{resolvedType.Name}' does not implement interface '{interfaceConstraint.Name}' required for generic parameter '{constraint.ParameterName}'");
                }
            }
        }
    }
}

/// <summary>
/// Contains the results of generic type resolution for a test
/// </summary>
internal sealed class TestGenericTypeResolution
{
    /// <summary>
    /// Resolved generic type arguments for the test class.
    /// Will be Type.EmptyTypes if the class is not generic.
    /// </summary>
    public Type[] ResolvedClassGenericArguments { get; set; } = Type.EmptyTypes;

    /// <summary>
    /// Resolved generic type arguments for the test method.
    /// Will be Type.EmptyTypes if the method is not generic.
    /// </summary>
    public Type[] ResolvedMethodGenericArguments { get; set; } = Type.EmptyTypes;

    /// <summary>
    /// Gets whether any generic types were resolved
    /// </summary>
    public bool HasResolvedTypes => 
        ResolvedClassGenericArguments.Length > 0 || 
        ResolvedMethodGenericArguments.Length > 0;
}