using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Handles generic type resolution and instantiation for reflection-based test discovery
/// </summary>
[RequiresUnreferencedCode("Uses reflection to analyze and instantiate generic types")]
[RequiresDynamicCode("Uses reflection to analyze and instantiate generic types")]
internal static class ReflectionGenericTypeResolver
{
    /// <summary>
    /// Determines generic type arguments from data row values
    /// </summary>
    public static Type[]? DetermineGenericTypeArguments(Type genericTypeDefinition, object?[] dataRow)
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new Exception("Using TUnit Reflection mechanisms isn't supported in AOT mode");
        }
#endif

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
    public static GenericTypeInfo? ExtractGenericTypeInfo(Type testClass)
    {
        // Handle both generic type definitions and constructed generic types
        Type typeToAnalyze;
        if (testClass.IsGenericTypeDefinition)
        {
            typeToAnalyze = testClass;
        }
        else if (testClass.IsConstructedGenericType)
        {
            // For constructed generic types (like Issue2952GenericBase<int>),
            // use the generic type definition to extract parameter names and constraints
            typeToAnalyze = testClass.GetGenericTypeDefinition();
        }
        else
        {
            return null;
        }

        var genericParams = typeToAnalyze.GetGenericArguments();
        var constraints = new GenericParameterConstraints[genericParams.Length];

        for (var i = 0; i < genericParams.Length; i++)
        {
            var param = genericParams[i];
            constraints[i] = new GenericParameterConstraints
            {
                ParameterName = param.Name,
                BaseTypeConstraint = param.BaseType != typeof(object) ? param.BaseType : null,
                InterfaceConstraints = AssemblyReferenceCache.GetInterfaces(param),
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
                InterfaceConstraints = AssemblyReferenceCache.GetInterfaces(param),
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

    /// <summary>
    /// Describes the first generic constraint of <paramref name="genericTypeDefinition"/> that
    /// <paramref name="typeArguments"/> break, or returns <c>null</c> when they satisfy every constraint.
    /// The wording matches <see cref="TUnit.Engine.Services.TestGenericTypeResolver"/> and the source
    /// generator, so both modes report the same failure message.
    /// </summary>
    public static string? FindConstraintViolation(Type genericTypeDefinition, Type[] typeArguments)
    {
        var genericParameters = genericTypeDefinition.GetGenericArguments();
        if (genericParameters.Length != typeArguments.Length)
        {
            return null;
        }

        var violation = DescribeConstraintViolation(genericParameters, typeArguments);
        if (violation != null)
        {
            return violation;
        }

        try
        {
            _ = genericTypeDefinition.MakeGenericType(typeArguments);
            return null;
        }
        catch (ArgumentException)
        {
            return $"Type arguments '{JoinTypeNames(typeArguments)}' do not satisfy the generic constraints of type '{genericTypeDefinition.Name}'";
        }
    }

    /// <summary>
    /// Describes the first generic constraint of <paramref name="genericMethodDefinition"/> that
    /// <paramref name="typeArguments"/> break, or returns <c>null</c> when they satisfy every constraint.
    /// </summary>
    /// <remarks>
    /// A method constraint may name a type parameter of the declaring class
    /// (<c>where U : IEnumerable&lt;T&gt;</c>). Its constraints still name the open class parameter even when
    /// the method comes from a constructed class, so the class's type arguments are substituted in first,
    /// as the source generator does.
    /// </remarks>
    public static string? FindConstraintViolation(MethodInfo genericMethodDefinition, Type[] typeArguments)
    {
        var genericParameters = genericMethodDefinition.GetGenericArguments();
        if (genericParameters.Length != typeArguments.Length)
        {
            return null;
        }

        var substitutionParameters = genericParameters;
        var substitutionArguments = typeArguments;
        if (genericMethodDefinition.DeclaringType is { IsConstructedGenericType: true } declaringType)
        {
            var classGenericParameters = declaringType.GetGenericTypeDefinition().GetGenericArguments();
            var classTypeArguments = declaringType.GenericTypeArguments;
            if (classGenericParameters.Length == classTypeArguments.Length)
            {
                substitutionParameters = classGenericParameters.Concat(genericParameters).ToArray();
                substitutionArguments = classTypeArguments.Concat(typeArguments).ToArray();
            }
        }

        var violation = DescribeConstraintViolation(genericParameters, typeArguments, substitutionParameters, substitutionArguments);
        if (violation != null)
        {
            return violation;
        }

        try
        {
            _ = genericMethodDefinition.MakeGenericMethod(typeArguments);
            return null;
        }
        catch (ArgumentException)
        {
            return $"Type arguments '{JoinTypeNames(typeArguments)}' do not satisfy the generic constraints of method '{genericMethodDefinition.Name}'";
        }
    }

    private static string? DescribeConstraintViolation(Type[] genericParameters, Type[] typeArguments)
        => DescribeConstraintViolation(genericParameters, typeArguments, genericParameters, typeArguments);

    /// <param name="genericParameters">The generic parameters whose constraints are checked.</param>
    /// <param name="typeArguments">The type arguments for <paramref name="genericParameters"/>.</param>
    /// <param name="substitutionParameters">Every generic parameter a constraint may name (the
    /// declaring class's as well as <paramref name="genericParameters"/>).</param>
    /// <param name="substitutionArguments">The type arguments for <paramref name="substitutionParameters"/>.</param>
    private static string? DescribeConstraintViolation(Type[] genericParameters, Type[] typeArguments, Type[] substitutionParameters, Type[] substitutionArguments)
    {
        for (var i = 0; i < genericParameters.Length; i++)
        {
            var genericParameter = genericParameters[i];
            var typeArgument = typeArguments[i];
            var attributes = genericParameter.GenericParameterAttributes;

            // Nullable<T> is a value type but never satisfies 'struct'.
            if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0
                && (!typeArgument.IsValueType || Nullable.GetUnderlyingType(typeArgument) != null))
            {
                return $"Type '{typeArgument.Name}' does not satisfy the 'struct' constraint for generic parameter '{genericParameter.Name}'";
            }

            if ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && typeArgument.IsValueType)
            {
                return $"Type '{typeArgument.Name}' does not satisfy the 'class' constraint for generic parameter '{genericParameter.Name}'";
            }

            var constraints = genericParameter.GetGenericParameterConstraints();

            // The base type constraint is checked before interface constraints, as in the engine. A
            // constraint naming another generic parameter (`where T : U`) is checked against U's type
            // argument, classified by what that argument is, as the source generator does.
            foreach (var constraint in constraints)
            {
                if (constraint == typeof(ValueType))
                {
                    continue;
                }

                var substituted = SubstituteGenericParameters(constraint, substitutionParameters, substitutionArguments);
                if (substituted != null && !substituted.IsInterface && !substituted.IsAssignableFrom(typeArgument))
                {
                    return $"Type '{typeArgument.Name}' does not satisfy the base type constraint '{substituted.Name}' for generic parameter '{genericParameter.Name}'";
                }
            }

            foreach (var constraint in constraints)
            {
                var substituted = SubstituteGenericParameters(constraint, substitutionParameters, substitutionArguments);
                if (substituted != null && substituted.IsInterface && !substituted.IsAssignableFrom(typeArgument))
                {
                    return $"Type '{typeArgument.Name}' does not implement interface '{substituted.Name}' required for generic parameter '{genericParameter.Name}'";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Replaces the generic parameters in <paramref name="type"/> (for example, <c>IComparable&lt;T&gt;</c>)
    /// with their type arguments. Returns <c>null</c> when the type cannot be constructed.
    /// </summary>
    private static Type? SubstituteGenericParameters(Type type, Type[] genericParameters, Type[] typeArguments)
    {
        if (!type.ContainsGenericParameters)
        {
            return type;
        }

        if (type.IsGenericParameter)
        {
            var index = Array.IndexOf(genericParameters, type);
            return index >= 0 ? typeArguments[index] : null;
        }

        // An array of a generic parameter (the U[] in IEnumerable<U[]>): substitute the element type,
        // as the source generator does, so the specific constraint message is reported.
        if (type.IsArray)
        {
            var element = SubstituteGenericParameters(type.GetElementType()!, genericParameters, typeArguments);
            if (element == null)
            {
                return null;
            }

#if NET
            var isVector = type.IsSZArray;
#else
            var isVector = type.Name.EndsWith("[]", StringComparison.Ordinal);
#endif
            return isVector ? element.MakeArrayType() : element.MakeArrayType(type.GetArrayRank());
        }

        if (!type.IsGenericType)
        {
            return null;
        }

        var arguments = type.GetGenericArguments();
        for (var i = 0; i < arguments.Length; i++)
        {
            var substituted = SubstituteGenericParameters(arguments[i], genericParameters, typeArguments);
            if (substituted == null)
            {
                return null;
            }

            arguments[i] = substituted;
        }

        try
        {
            return type.GetGenericTypeDefinition().MakeGenericType(arguments);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string JoinTypeNames(Type[] types) => string.Join(", ", types.Select(static t => t.Name));
}
