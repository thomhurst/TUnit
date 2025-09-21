using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

/// <summary>
/// Helper class for handling special floating-point values (NaN, Infinity) in code generation.
/// </summary>
internal static class SpecialFloatingPointValuesHelper
{
    /// <summary>
    /// Formats a floating-point value as a string for code generation, 
    /// handling special values like NaN and Infinity.
    /// </summary>
    public static string? TryFormatSpecialFloatingPointValue(object? value)
    {
        if (value == null)
            return null;

        // Handle float values
        if (value is float floatValue)
        {
            if (float.IsNaN(floatValue))
                return "float.NaN";
            if (float.IsPositiveInfinity(floatValue))
                return "float.PositiveInfinity";
            if (float.IsNegativeInfinity(floatValue))
                return "float.NegativeInfinity";
        }
        
        // Handle double values
        if (value is double doubleValue)
        {
            if (double.IsNaN(doubleValue))
                return "double.NaN";
            if (double.IsPositiveInfinity(doubleValue))
                return "double.PositiveInfinity";
            if (double.IsNegativeInfinity(doubleValue))
                return "double.NegativeInfinity";
        }

        return null;
    }

    /// <summary>
    /// Creates a SyntaxNode for a special floating-point value using member access expressions.
    /// Returns null if the value is not a special floating-point value.
    /// </summary>
    public static SyntaxNode? TryCreateSpecialFloatingPointSyntax(object? value)
    {
        return value switch
        {
            float f when float.IsNaN(f) => CreateFloatMemberAccess("NaN"),
            float f when float.IsPositiveInfinity(f) => CreateFloatMemberAccess("PositiveInfinity"),
            float f when float.IsNegativeInfinity(f) => CreateFloatMemberAccess("NegativeInfinity"),
            double d when double.IsNaN(d) => CreateDoubleMemberAccess("NaN"),
            double d when double.IsPositiveInfinity(d) => CreateDoubleMemberAccess("PositiveInfinity"),
            double d when double.IsNegativeInfinity(d) => CreateDoubleMemberAccess("NegativeInfinity"),
            _ => null
        };
    }

    private static MemberAccessExpressionSyntax CreateFloatMemberAccess(string memberName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
            SyntaxFactory.IdentifierName(memberName));
    }

    private static MemberAccessExpressionSyntax CreateDoubleMemberAccess(string memberName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword)),
            SyntaxFactory.IdentifierName(memberName));
    }
}