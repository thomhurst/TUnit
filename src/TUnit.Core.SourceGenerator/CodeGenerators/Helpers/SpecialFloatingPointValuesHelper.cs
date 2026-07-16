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
        return value switch
        {
            null => null,
            // Handle float values
            float.NaN => "float.NaN",
            float floatValue when float.IsPositiveInfinity(floatValue) => "float.PositiveInfinity",
            float floatValue when float.IsNegativeInfinity(floatValue) => "float.NegativeInfinity",
            // Handle double values
            double.NaN => "double.NaN",
            double doubleValue when double.IsPositiveInfinity(doubleValue) => "double.PositiveInfinity",
            double doubleValue when double.IsNegativeInfinity(doubleValue) => "double.NegativeInfinity",
            _ => null
        };
    }

    /// <summary>
    /// Creates a SyntaxNode for a special floating-point value using member access expressions.
    /// Returns null if the value is not a special floating-point value.
    /// </summary>
    public static SyntaxNode? TryCreateSpecialFloatingPointSyntax(object? value)
    {
        return value switch
        {
            float.NaN => CreateFloatMemberAccess("NaN"),
            float f when float.IsPositiveInfinity(f) => CreateFloatMemberAccess("PositiveInfinity"),
            float f when float.IsNegativeInfinity(f) => CreateFloatMemberAccess("NegativeInfinity"),
            double.NaN => CreateDoubleMemberAccess("NaN"),
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
