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
    /// Checks if a type has conversion operators that might not work in AOT
    /// </summary>
    public static bool HasConversionOperators(ITypeSymbol type)
    {
        var members = type.GetMembers();
        return members.Any(m => m is IMethodSymbol method && 
            (method.Name == "op_Implicit" || method.Name == "op_Explicit") &&
            method.IsStatic);
    }

    /// <summary>
    /// Gets all conversion operators for a type
    /// </summary>
    public static IEnumerable<(IMethodSymbol method, ITypeSymbol targetType)> GetConversionOperators(ITypeSymbol type)
    {
        var members = type.GetMembers();
        foreach (var member in members)
        {
            if (member is IMethodSymbol method && 
                (method.Name == "op_Implicit" || method.Name == "op_Explicit") &&
                method is { IsStatic: true, Parameters.Length: 1 })
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