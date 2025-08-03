using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Handles generic type resolution and instantiation for reflection-based test discovery
/// </summary>
[RequiresUnreferencedCode("Reflection-based generic type resolution requires unreferenced code")]
internal static class ReflectionGenericTypeResolver
{
    /// <summary>
    /// Determines generic type arguments from data row values
    /// </summary>
    public static Type[]? DetermineGenericTypeArguments(Type genericTypeDefinition, object?[] dataRow)
    {
        var genericParameters = genericTypeDefinition.GetGenericArguments();

        // If no data row or empty data, can't determine types
        if (dataRow.Length == 0)
        {
            return null;
        }

        var typeArguments = new Type[genericParameters.Length];

        // For generic classes with constructors, we need to infer types from constructor parameters
        // We should match the number of generic parameters, not the number of data items
        if (genericParameters.Length == 1 && dataRow.Length >= 1)
        {
            // Single generic parameter - use first non-null argument's type
            for (var i = 0; i < dataRow.Length; i++)
            {
                if (dataRow[i] != null)
                {
                    typeArguments[0] = dataRow[i]!.GetType();
                    break;
                }
            }

            // If we couldn't determine the type, return null
            if (typeArguments[0] == null)
            {
                return null;
            }
        }
        else
        {
            // Multiple generic parameters - try to match one-to-one with data
            for (var i = 0; i < genericParameters.Length; i++)
            {
                if (i < dataRow.Length && dataRow[i] != null)
                {
                    typeArguments[i] = dataRow[i]!.GetType();
                }
                else
                {
                    // Can't determine all generic types
                    return null;
                }
            }
        }

        return typeArguments;
    }

    /// <summary>
    /// Extracts generic type information including constraints
    /// </summary>
    [UnconditionalSuppressMessage("Trimming",
        "IL2065:Value passed to implicit 'this' parameter of method 'System.Type.GetInterfaces()' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements",
        Justification = "Reflection mode requires dynamic access")]
    public static GenericTypeInfo? ExtractGenericTypeInfo(Type testClass)
    {
        if (!testClass.IsGenericTypeDefinition)
        {
            return null;
        }

        var genericParams = testClass.GetGenericArguments();
        var constraints = new GenericParameterConstraints[genericParams.Length];

        for (var i = 0; i < genericParams.Length; i++)
        {
            var param = genericParams[i];
            constraints[i] = new GenericParameterConstraints
            {
                ParameterName = param.Name,
                BaseTypeConstraint = param.BaseType != typeof(object) ? param.BaseType : null,
                InterfaceConstraints = param.GetInterfaces(),
                HasDefaultConstructorConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint),
                HasReferenceTypeConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint),
                HasValueTypeConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint),
                HasNotNullConstraint = false // .NET doesn't expose this via reflection
            };
        }

        return new GenericTypeInfo
        {
            ParameterNames = genericParams.Select(p => p.Name).ToArray(),
            Constraints = constraints
        };
    }

    /// <summary>
    /// Extracts generic method information including parameter positions
    /// </summary>
    [UnconditionalSuppressMessage("Trimming",
        "IL2065:Value passed to implicit 'this' parameter of method 'System.Type.GetInterfaces()' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements",
        Justification = "Reflection mode requires dynamic access")]
    public static GenericMethodInfo? ExtractGenericMethodInfo(MethodInfo method)
    {
        if (!method.IsGenericMethodDefinition)
        {
            return null;
        }

        var genericParams = method.GetGenericArguments();
        var constraints = new GenericParameterConstraints[genericParams.Length];
        var parameterPositions = new List<int>();

        // Map generic parameters to method argument positions
        var methodParams = method.GetParameters();
        for (var i = 0; i < methodParams.Length; i++)
        {
            var paramType = methodParams[i].ParameterType;
            if (paramType.IsGenericParameter && Array.IndexOf(genericParams, paramType) >= 0)
            {
                parameterPositions.Add(i);
            }
        }

        for (var i = 0; i < genericParams.Length; i++)
        {
            var param = genericParams[i];
            constraints[i] = new GenericParameterConstraints
            {
                ParameterName = param.Name,
                BaseTypeConstraint = param.BaseType != typeof(object) ? param.BaseType : null,
                InterfaceConstraints = param.GetInterfaces(),
                HasDefaultConstructorConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint),
                HasReferenceTypeConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint),
                HasValueTypeConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint),
                HasNotNullConstraint = false // .NET doesn't expose this via reflection
            };
        }

        return new GenericMethodInfo
        {
            ParameterNames = genericParams.Select(p => p.Name).ToArray(),
            Constraints = constraints,
            ParameterPositions = parameterPositions.ToArray()
        };
    }

    /// <summary>
    /// Creates a concrete type from a generic type definition and validates the type arguments
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2055:Call to 'System.Type.MakeGenericType' can not be statically analyzed",
        Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "Reflection mode cannot support AOT")]
    public static Type CreateConcreteType(Type genericTypeDefinition, Type[] typeArguments)
    {
        var genericParams = genericTypeDefinition.GetGenericArguments();
        if (typeArguments.Length != genericParams.Length)
        {
            throw new InvalidOperationException(
                $"Type argument count mismatch: {genericTypeDefinition.Name} expects {genericParams.Length} type arguments but got {typeArguments.Length}. " +
                $"Generic parameters: [{string.Join(", ", genericParams.Select(p => p.Name))}], " +
                $"Type arguments: [{string.Join(", ", typeArguments.Select(t => t.Name))}]");
        }

        return genericTypeDefinition.MakeGenericType(typeArguments);
    }
}