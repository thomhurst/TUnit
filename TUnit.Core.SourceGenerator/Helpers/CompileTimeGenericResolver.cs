using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Helpers;

/// <summary>
/// Resolves generic type parameters at compile time based on argument types
/// </summary>
internal static class CompileTimeGenericResolver
{
    public static Dictionary<ITypeParameterSymbol, ITypeSymbol>? ResolveGenericArguments(
        IMethodSymbol genericMethod,
        ITypeSymbol[] argumentTypes)
    {
        if (!genericMethod.IsGenericMethod)
        {
            return null;
        }

        var typeMapping = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        var parameters = genericMethod.Parameters;

        // Match parameters with arguments
        for (int i = 0; i < Math.Min(parameters.Length, argumentTypes.Length); i++)
        {
            var parameterType = parameters[i].Type;
            var argumentType = argumentTypes[i];

            if (!TryInferTypeMapping(parameterType, argumentType, typeMapping))
            {
                return null; // Cannot resolve
            }
        }

        // Verify all type parameters are resolved
        foreach (var typeParam in genericMethod.TypeParameters)
        {
            if (!typeMapping.ContainsKey(typeParam))
            {
                return null; // Unresolved type parameter
            }
        }

        return typeMapping;
    }

    private static bool TryInferTypeMapping(
        ITypeSymbol parameterType,
        ITypeSymbol argumentType,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> typeMapping)
    {
        // Direct type parameter
        if (parameterType is ITypeParameterSymbol typeParam)
        {
            if (typeMapping.TryGetValue(typeParam, out var existingMapping))
            {
                // Verify consistency
                return SymbolEqualityComparer.Default.Equals(existingMapping, argumentType);
            }
            else
            {
                typeMapping[typeParam] = argumentType;
                return true;
            }
        }

        // Array types
        if (parameterType is IArrayTypeSymbol paramArray && argumentType is IArrayTypeSymbol argArray)
        {
            return TryInferTypeMapping(paramArray.ElementType, argArray.ElementType, typeMapping);
        }

        // Generic types (e.g., List<T>, Dictionary<K,V>)
        if (parameterType is INamedTypeSymbol paramNamed &&
            argumentType is INamedTypeSymbol argNamed &&
            paramNamed.IsGenericType &&
            argNamed.IsGenericType)
        {
            // Check if they're the same generic type definition
            if (!SymbolEqualityComparer.Default.Equals(
                paramNamed.OriginalDefinition,
                argNamed.OriginalDefinition))
            {
                return false;
            }

            // Recursively match type arguments
            var paramTypeArgs = paramNamed.TypeArguments;
            var argTypeArgs = argNamed.TypeArguments;

            if (paramTypeArgs.Length != argTypeArgs.Length)
            {
                return false;
            }

            for (int i = 0; i < paramTypeArgs.Length; i++)
            {
                if (!TryInferTypeMapping(paramTypeArgs[i], argTypeArgs[i], typeMapping))
                {
                    return false;
                }
            }

            return true;
        }

        // Non-generic types must be compatible
        return IsAssignableFrom(parameterType, argumentType);
    }

    private static bool IsAssignableFrom(ITypeSymbol targetType, ITypeSymbol sourceType)
    {
        if (SymbolEqualityComparer.Default.Equals(targetType, sourceType))
        {
            return true;
        }

        // Check base types
        var currentBase = sourceType.BaseType;
        while (currentBase != null)
        {
            if (SymbolEqualityComparer.Default.Equals(targetType, currentBase))
            {
                return true;
            }
            currentBase = currentBase.BaseType;
        }

        // Check interfaces
        foreach (var iface in sourceType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(targetType, iface))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ValidateConstraints(
        IMethodSymbol genericMethod,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> typeMapping)
    {
        foreach (var typeParam in genericMethod.TypeParameters)
        {
            if (!typeMapping.TryGetValue(typeParam, out var resolvedType))
            {
                return false;
            }

            // Check constraints
            if (typeParam.HasReferenceTypeConstraint && resolvedType.IsValueType)
            {
                return false;
            }

            if (typeParam.HasValueTypeConstraint && !resolvedType.IsValueType)
            {
                return false;
            }

            if (typeParam.HasConstructorConstraint)
            {
                // Check for public parameterless constructor
                if (resolvedType.IsAbstract || resolvedType.IsStatic)
                {
                    return false;
                }

                if (!resolvedType.IsValueType && resolvedType is INamedTypeSymbol namedType)
                {
                    var hasParameterlessConstructor = namedType.Constructors
                        .Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);

                    if (!hasParameterlessConstructor)
                    {
                        return false;
                    }
                }
            }

            // Check type constraints
            foreach (var constraintType in typeParam.ConstraintTypes)
            {
                if (!IsAssignableFrom(constraintType, resolvedType))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static string GetGenericInstantiationSuffix(Dictionary<ITypeParameterSymbol, ITypeSymbol> typeMapping)
    {
        var typeNames = typeMapping.Values.Select(t => GetSimpleTypeName(t));
        return string.Join("_", typeNames);
    }

    private static string GetSimpleTypeName(ITypeSymbol type)
    {
        // Handle special cases
        return type.SpecialType switch
        {
            SpecialType.System_Int32 => "Int32",
            SpecialType.System_String => "String",
            SpecialType.System_Boolean => "Boolean",
            SpecialType.System_Double => "Double",
            SpecialType.System_Single => "Single",
            _ => type.Name.Replace("`", "_")
        };
    }
}
