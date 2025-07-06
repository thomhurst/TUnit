using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// Implementation of generic type resolution for test methods and classes
/// </summary>
[RequiresDynamicCode("Generic type resolution requires runtime type generation")]
[RequiresUnreferencedCode("Generic type resolution may access types not preserved by trimming")]
public class GenericTypeResolver : IGenericTypeResolver
{
    /// <inheritdoc />
    public Type[] ResolveGenericMethodArguments(MethodInfo genericMethodDefinition, object?[] runtimeArguments)
    {
        if (!genericMethodDefinition.IsGenericMethodDefinition)
        {
            throw new ArgumentException("Method is not a generic method definition", nameof(genericMethodDefinition));
        }

        var genericParameters = genericMethodDefinition.GetGenericArguments();
        var methodParameters = genericMethodDefinition.GetParameters();
        var typeMapping = new Dictionary<Type, Type>();

        // Infer types from arguments
        for (var i = 0; i < methodParameters.Length && i < runtimeArguments.Length; i++)
        {
            var parameterType = methodParameters[i].ParameterType;
            var argumentValue = runtimeArguments[i];

            if (argumentValue != null)
            {
                var argumentType = argumentValue.GetType();
                InferTypeMapping(parameterType, argumentType, typeMapping);
            }
            else if (parameterType.IsGenericParameter)
            {
                // For null arguments with generic parameters, we need more context
                // Try to infer from constraints or other arguments
                var constraints = parameterType.GetGenericParameterConstraints();
                if (constraints is
                    [
                        { IsInterface: false }
                    ])
                {
                    // If there's exactly one non-interface constraint, we might be able to use it
                    typeMapping[parameterType] = constraints[0];
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
                // Try to use constraints as a fallback
                var constraints = genericParam.GetGenericParameterConstraints();
                var attributes = genericParam.GenericParameterAttributes;

                // Provide more specific error message with context
                var methodName = $"{genericMethodDefinition.DeclaringType?.FullName}.{genericMethodDefinition.Name}";
                var argInfo = runtimeArguments.Length > 0
                    ? $"Arguments: {string.Join(", ", runtimeArguments.Select(a => a?.GetType()?.Name ?? "null"))}"
                    : "No arguments provided";

                throw new GenericTypeResolutionException(
                    $"Could not resolve type for generic parameter '{genericParam.Name}' in method '{methodName}'. " +
                    $"{argInfo}. " +
                    "Ensure test arguments provide enough type information to infer all generic parameters.");
            }
            resolvedTypes[i] = resolvedType;
        }

        // Validate constraints
        ValidateGenericConstraints(genericParameters, resolvedTypes);

        return resolvedTypes;
    }

    /// <inheritdoc />
    public Type[] ResolveGenericClassArguments(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type genericTypeDefinition,
        object?[] constructorArguments)
    {
        if (!genericTypeDefinition.IsGenericTypeDefinition)
        {
            throw new ArgumentException("Type is not a generic type definition", nameof(genericTypeDefinition));
        }

        var genericParameters = genericTypeDefinition.GetGenericArguments();
        var constructors = genericTypeDefinition.GetConstructors();
        var typeMapping = new Dictionary<Type, Type>();

        // Try to infer from constructor parameters
        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length == constructorArguments.Length)
            {
                typeMapping.Clear();
                var allMatched = true;

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameterType = parameters[i].ParameterType;
                    var argumentValue = constructorArguments[i];

                    if (argumentValue != null)
                    {
                        var argumentType = argumentValue.GetType();
                        if (!TryInferTypeMapping(parameterType, argumentType, typeMapping))
                        {
                            allMatched = false;
                            break;
                        }
                    }
                }

                if (allMatched && typeMapping.Count == genericParameters.Length)
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
                    $"Could not resolve type for generic parameter '{genericParam.Name}' of type '{genericTypeDefinition.Name}'. " +
                    "Ensure constructor arguments provide enough type information.");
            }
            resolvedTypes[i] = resolvedType;
        }

        // Validate constraints
        ValidateGenericConstraints(genericParameters, resolvedTypes);

        return resolvedTypes;
    }

    private void InferTypeMapping(Type parameterType, Type argumentType, Dictionary<Type, Type> typeMapping)
    {
        if (!TryInferTypeMapping(parameterType, argumentType, typeMapping))
        {
            throw new GenericTypeResolutionException(
                $"Type mismatch: Cannot use argument of type '{argumentType.Name}' for parameter of type '{parameterType.Name}'");
        }
    }

    private bool TryInferTypeMapping(Type parameterType, Type argumentType, Dictionary<Type, Type> typeMapping)
    {
        // Direct generic parameter
        if (parameterType.IsGenericParameter)
        {
            if (typeMapping.TryGetValue(parameterType, out var existingMapping))
            {
                // Verify consistency
                if (existingMapping != argumentType)
                {
                    throw new GenericTypeResolutionException(
                        $"Inconsistent type inference for generic parameter '{parameterType.Name}': " +
                        $"previously inferred as '{existingMapping.Name}', now '{argumentType.Name}'");
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
            return TryInferTypeMapping(parameterType.GetElementType()!, argumentType.GetElementType()!, typeMapping);
        }

        // Generic types (e.g., List<T>, Dictionary<K,V>)
        if (parameterType.IsGenericType && argumentType.IsGenericType)
        {
            var paramGenericDef = parameterType.GetGenericTypeDefinition();
            var argGenericDef = argumentType.IsGenericTypeDefinition ? argumentType : argumentType.GetGenericTypeDefinition();

            if (paramGenericDef == argGenericDef)
            {
                var paramGenericArgs = parameterType.GetGenericArguments();
                var argGenericArgs = argumentType.GetGenericArguments();

                if (paramGenericArgs.Length == argGenericArgs.Length)
                {
                    var allMatched = true;
                    for (var i = 0; i < paramGenericArgs.Length; i++)
                    {
                        if (!TryInferTypeMapping(paramGenericArgs[i], argGenericArgs[i], typeMapping))
                        {
                            allMatched = false;
                            break;
                        }
                    }
                    return allMatched;
                }
            }
        }

        // Non-generic types must match exactly or be assignable
        return parameterType.IsAssignableFrom(argumentType);
    }

    private void ValidateGenericConstraints(Type[] genericParameters, Type[] resolvedTypes)
    {
        for (var i = 0; i < genericParameters.Length; i++)
        {
            var genericParam = genericParameters[i];
            var resolvedType = resolvedTypes[i];
            var attributes = genericParam.GenericParameterAttributes;

            // Check class constraint
            if ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            {
                if (!resolvedType.IsClass)
                {
                    throw new GenericTypeResolutionException(
                        $"Type '{resolvedType.Name}' does not satisfy the 'class' constraint for generic parameter '{genericParam.Name}'");
                }
            }

            // Check struct constraint
            if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            {
                if (!resolvedType.IsValueType || resolvedType == typeof(Nullable<>))
                {
                    throw new GenericTypeResolutionException(
                        $"Type '{resolvedType.Name}' does not satisfy the 'struct' constraint for generic parameter '{genericParam.Name}'");
                }
            }

            // Check new() constraint
            if ((attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
            {
                if (resolvedType.IsAbstract)
                {
                    throw new GenericTypeResolutionException(
                        $"Type '{resolvedType.Name}' does not satisfy the 'new()' constraint for generic parameter '{genericParam.Name}' - type is abstract");
                }

                // For value types, they always have a default constructor
                if (!resolvedType.IsValueType)
                {
                    // For reference types in reflection mode, we check if Activator.CreateInstance would work
                    // This is still AOT-compatible as we're not actually creating an instance
                    try
                    {
                        // For AOT compatibility, we avoid GetConstructor and instead check if we can create an instance
                        // The actual constraint validation will happen at compile time in source gen mode
                        if (!HasPublicParameterlessConstructor(resolvedType))
                        {
                            throw new GenericTypeResolutionException(
                                $"Type '{resolvedType.Name}' does not satisfy the 'new()' constraint for generic parameter '{genericParam.Name}' - no public parameterless constructor");
                        }
                    }
                    catch (Exception ex) when (ex is not GenericTypeResolutionException)
                    {
                        throw new GenericTypeResolutionException(
                            $"Type '{resolvedType.Name}' does not satisfy the 'new()' constraint for generic parameter '{genericParam.Name}'", ex);
                    }
                }
            }

            // Check type constraints (base class and interfaces)
            var constraints = genericParam.GetGenericParameterConstraints();
            foreach (var constraint in constraints)
            {
                if (!constraint.IsAssignableFrom(resolvedType))
                {
                    throw new GenericTypeResolutionException(
                        $"Type '{resolvedType.Name}' does not satisfy the constraint '{constraint.Name}' for generic parameter '{genericParam.Name}'");
                }
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Type is preserved through DynamicallyAccessedMembers")]
    private static bool HasPublicParameterlessConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
    {
        // This method is designed to be AOT-friendly
        // In source gen mode, this check happens at compile time
        // In reflection mode, we need to check at runtime
        return type.GetConstructor(Type.EmptyTypes) != null;
    }
}
