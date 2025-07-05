using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Helpers;

/// <summary>
/// Generates compile-time code for converting data source return types to object?[] arrays.
/// </summary>
public static class CompileTimeArgumentConverter
{
    /// <summary>
    /// Generates a C# expression that converts data from a data source to IEnumerable<object?[]>.
    /// </summary>
    public static string GenerateConversionExpression(
        ITypeSymbol sourceType,
        IMethodSymbol targetMethod,
        string dataVariableName)
    {
        // Extract the element type from IEnumerable<T>
        var enumerableType = GetEnumerableElementType(sourceType);
        if (enumerableType == null)
        {
            // Not an IEnumerable - generate code that throws at runtime
            return $"new[] {{ System.Array.Empty<object?>() }} /* ERROR: Data source must return IEnumerable<T>, but returns {sourceType.ToDisplayString()} */";
        }

        // If already object?[], no conversion needed
        if (IsObjectArray(enumerableType))
        {
            return dataVariableName;
        }

        // Handle single value to single parameter
        if (targetMethod.Parameters.Length == 1 && !(enumerableType is INamedTypeSymbol nt && IsTupleType(nt)))
        {
            return $"{dataVariableName}.Select(x => new object?[] {{ x }})";
        }

        // Handle tuple types
        if (enumerableType is INamedTypeSymbol namedType && IsTupleType(namedType))
        {
            return GenerateTupleConversion(namedType, targetMethod, dataVariableName);
        }

        // Handle custom types by trying to match properties/fields to parameters
        if (enumerableType is INamedTypeSymbol customType)
        {
            return GenerateCustomTypeConversion(customType, targetMethod, dataVariableName);
        }

        // Default: wrap single value
        return $"{dataVariableName}.Select(x => new object?[] {{ x }})";
    }

    private static ITypeSymbol? GetEnumerableElementType(ITypeSymbol type)
    {
        // Check if it's IEnumerable<T>
        var enumerableInterface = type.AllInterfaces
            .FirstOrDefault(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

        if (enumerableInterface != null)
        {
            return enumerableInterface.TypeArguments[0];
        }

        // Check if the type itself is IEnumerable<T>
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
        {
            return namedType.TypeArguments[0];
        }

        return null;
    }

    private static bool IsObjectArray(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType.SpecialType == SpecialType.System_Object;
        }
        return false;
    }

    private static bool IsTupleType(INamedTypeSymbol type)
    {
        return type.IsTupleType ||
               type.Name.StartsWith("ValueTuple`") ||
               type.Name.StartsWith("Tuple`");
    }

    private static string GenerateTupleConversion(
        INamedTypeSymbol tupleType,
        IMethodSymbol targetMethod,
        string dataVariableName)
    {
        var tupleElements = GetTupleElements(tupleType);

        if (tupleElements.Length != targetMethod.Parameters.Length)
        {
            // Mismatch in count, generate error comment
            // Generate compile-time comment about the mismatch but still compile
            return $"{dataVariableName}.Select(x => new object?[] {{ /* COMPILE ERROR: Tuple has {tupleElements.Length} elements but method expects {targetMethod.Parameters.Length} parameters */ }})";
        }

        // Generate: data.Select(x => new object?[] { x.Item1, x.Item2, ... })
        var itemAccessors = string.Join(", ",
            Enumerable.Range(1, tupleElements.Length).Select(i => $"x.Item{i}"));

        return $"{dataVariableName}.Select(x => new object?[] {{ {itemAccessors} }})";
    }

    private static ITypeSymbol[] GetTupleElements(INamedTypeSymbol tupleType)
    {
        if (tupleType.IsTupleType)
        {
            return tupleType.TupleElements.Select(f => f.Type).ToArray();
        }

        // For System.Tuple or System.ValueTuple
        return tupleType.TypeArguments.ToArray();
    }

    private static string GenerateCustomTypeConversion(
        INamedTypeSymbol customType,
        IMethodSymbol targetMethod,
        string dataVariableName)
    {
        // Try to match properties to method parameters by name
        var parameterNames = targetMethod.Parameters.Select(p => p.Name.ToLowerInvariant()).ToArray();
        var properties = customType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .ToArray();

        var propertyAccessors = new string?[targetMethod.Parameters.Length];

        for (var i = 0; i < targetMethod.Parameters.Length; i++)
        {
            var paramName = parameterNames[i];
            var matchingProperty = properties.FirstOrDefault(p =>
                p.Name.ToLowerInvariant() == paramName);

            if (matchingProperty != null)
            {
                propertyAccessors[i] = $"x.{matchingProperty.Name}";
            }
            else
            {
                // Try to find by index if numeric suffix
                if (i < properties.Length)
                {
                    propertyAccessors[i] = $"x.{properties[i].Name}";
                }
                else
                {
                    // No matching property found - use null
                    propertyAccessors[i] = "null";
                }
            }
        }

        var accessorList = string.Join(", ", propertyAccessors);
        return $"{dataVariableName}.Select(x => new object?[] {{ {accessorList} }})";
    }
}
