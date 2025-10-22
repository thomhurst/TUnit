using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

/// <summary>
/// Helper for generating AOT-compatible type conversions
/// </summary>
public static class AotConversionHelper
{
    /// <summary>
    /// Generates an AOT-compatible conversion expression
    /// </summary>
    /// <param name="sourceType">The source type</param>
    /// <param name="targetType">The target type</param>
    /// <param name="sourceExpression">The expression to convert</param>
    /// <returns>An AOT-compatible conversion expression or null if no special handling is needed</returns>
    public static string? GenerateAotConversion(ITypeSymbol sourceType, ITypeSymbol targetType, string sourceExpression)
    {
        // Check if direct assignment is possible
        if (sourceType.Equals(targetType, SymbolEqualityComparer.Default))
        {
            return sourceExpression;
        }

        // Handle nullable types
        var underlyingTargetType = targetType.NullableAnnotation == NullableAnnotation.Annotated || targetType.SpecialType == SpecialType.System_Nullable_T
            ? ((INamedTypeSymbol)targetType).TypeArguments.FirstOrDefault() ?? targetType
            : targetType;

        // Enum conversions - use direct cast instead of Enum.ToObject
        if (underlyingTargetType.TypeKind == TypeKind.Enum)
        {
            // For enums, generate a direct cast from the source value
            // This avoids using Enum.ToObject which is not AOT-compatible
            return $"({targetType.GloballyQualified()}){sourceExpression}";
        }

        // Array conversions - avoid Array.CreateInstance
        if (underlyingTargetType is IArrayTypeSymbol arrayType)
        {
            var elementType = arrayType.ElementType;

            // If source is also an array of the same element type, direct cast
            if (sourceType is IArrayTypeSymbol sourceArray &&
                sourceArray.ElementType.Equals(elementType, SymbolEqualityComparer.Default))
            {
                return sourceExpression;
            }

            // For single value to array conversion, generate array initialization syntax
            // Example: new int[] { (int)value }
            return $"(({sourceExpression}) is {targetType.GloballyQualified()} arr ? arr : new {elementType.GloballyQualified()}[] {{ ({elementType.GloballyQualified()}){sourceExpression} }})";
        }

        // Primitive type conversions - use direct casts when possible
        if (IsPrimitiveConversion(sourceType, underlyingTargetType))
        {
            return $"({targetType.GloballyQualified()}){sourceExpression}";
        }

        // Look for implicit conversion operators
        var implicitConversion = FindConversionOperator(sourceType, targetType, "op_Implicit");
        if (implicitConversion != null)
        {
            // For implicit conversions, we can use a simple cast
            return $"({targetType.GloballyQualified()})(({sourceType.GloballyQualified()}){sourceExpression})";
        }

        // Look for explicit conversion operators
        var explicitConversion = FindConversionOperator(sourceType, targetType, "op_Explicit");
        if (explicitConversion != null)
        {
            // For explicit conversions, we also use a cast
            return $"({targetType.GloballyQualified()})(({sourceType.GloballyQualified()}){sourceExpression})";
        }

        // No special AOT conversion needed, let CastHelper handle it
        return null;
    }

    /// <summary>
    /// Checks if a conversion between two types is a primitive conversion
    /// </summary>
    private static bool IsPrimitiveConversion(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        // Check if both are primitive types or one is object and the other is value type
        var isPrimitive = sourceType.SpecialType switch
        {
            SpecialType.System_Boolean => true,
            SpecialType.System_Char => true,
            SpecialType.System_SByte => true,
            SpecialType.System_Byte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt64 => true,
            SpecialType.System_Single => true,
            SpecialType.System_Double => true,
            SpecialType.System_Decimal => true,
            SpecialType.System_Object => targetType.IsValueType,
            _ => false
        };

        if (!isPrimitive)
        {
            return false;
        }

        var isTargetPrimitive = targetType.SpecialType switch
        {
            SpecialType.System_Boolean => true,
            SpecialType.System_Char => true,
            SpecialType.System_SByte => true,
            SpecialType.System_Byte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt64 => true,
            SpecialType.System_Single => true,
            SpecialType.System_Double => true,
            SpecialType.System_Decimal => true,
            _ => false
        };

        return isTargetPrimitive;
    }

    /// <summary>
    /// Checks if a type has conversion operators that might not work in AOT
    /// </summary>
    public static bool HasConversionOperators(ITypeSymbol type)
    {
        var members = type.GetMembers();
        return members.Any(m => m is IMethodSymbol { Name: "op_Implicit" or "op_Explicit", IsStatic: true });
    }

    /// <summary>
    /// Gets all conversion operators for a type
    /// </summary>
    public static IEnumerable<(IMethodSymbol method, ITypeSymbol targetType)> GetConversionOperators(ITypeSymbol type)
    {
        var members = type.GetMembers();
        foreach (var member in members)
        {
            if (member is IMethodSymbol { Name: "op_Implicit" or "op_Explicit" } method and { IsStatic: true, Parameters.Length: 1 })
            {
                yield return (method, method.ReturnType);
            }
        }
    }

    private static IMethodSymbol? FindConversionOperator(ITypeSymbol sourceType, ITypeSymbol targetType, string operatorName)
    {
        // Check operators in source type
        var sourceOperators = sourceType.GetMembers(operatorName)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && m.Parameters.Length == 1);

        foreach (var op in sourceOperators)
        {
            if (op.ReturnType.Equals(targetType, SymbolEqualityComparer.Default) &&
                op.Parameters[0].Type.Equals(sourceType, SymbolEqualityComparer.Default))
            {
                return op;
            }
        }

        // Check operators in target type
        var targetOperators = targetType.GetMembers(operatorName)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && m.Parameters.Length == 1);

        foreach (var op in targetOperators)
        {
            if (op.ReturnType.Equals(targetType, SymbolEqualityComparer.Default) &&
                op.Parameters[0].Type.Equals(sourceType, SymbolEqualityComparer.Default))
            {
                return op;
            }
        }

        return null;
    }
}