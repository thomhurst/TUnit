using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Formatting;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TypedConstantParser
{
    private static readonly TypedConstantFormatter _formatter = new();
    
    public static string GetTypedConstantValue(SemanticModel semanticModel,
        (TypedConstant typedConstant, AttributeArgumentSyntax a) element, ITypeSymbol? parameterType)
    {
        // For constant values, use the formatter which handles type conversions properly
        if (element.typedConstant.Kind == TypedConstantKind.Primitive)
        {
            return _formatter.FormatForCode(element.typedConstant, parameterType);
        }

        var argumentExpression = element.a.Expression;

        var newExpression = argumentExpression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(semanticModel))!;

        if (parameterType?.TypeKind == TypeKind.Enum &&
            (newExpression.IsKind(SyntaxKind.UnaryMinusExpression) || newExpression.IsKind(SyntaxKind.UnaryPlusExpression)))
        {
            return $"({parameterType.GloballyQualified()})({newExpression})";
        }

        if (parameterType?.SpecialType == SpecialType.System_Decimal)
        {
            // For decimal types, ensure we preserve precision by not relying on ToString()
            // which might have already lost precision. Use the original expression text.
            var expressionText = newExpression.ToString();
            if (expressionText.EndsWith("d") || expressionText.EndsWith("D"))
            {
                return expressionText.Substring(0, expressionText.Length - 1) + "m";
            }
            else if (!expressionText.EndsWith("m") && !expressionText.EndsWith("M"))
            {
                return expressionText + "m";
            }
            return expressionText;
        }

        if (parameterType is not null
            && element.typedConstant.Type is not null
            && semanticModel.Compilation.ClassifyConversion(element.typedConstant.Type, parameterType) is
            { IsExplicit: true, IsImplicit: false })
        {
            return $"({parameterType.GloballyQualified()})({newExpression})";
        }

        return newExpression.ToString();
    }

    public static string GetFullyQualifiedTypeNameFromTypedConstantValue(TypedConstant typedConstant)
    {
        if (typedConstant.Kind == TypedConstantKind.Type)
        {
            var type = (INamedTypeSymbol) typedConstant.Value!;
            return type.GloballyQualified();
        }

        if (typedConstant.Kind == TypedConstantKind.Enum)
        {
            return typedConstant.Type!.GloballyQualified();
        }

        if (typedConstant.Kind is not TypedConstantKind.Error and not TypedConstantKind.Array)
        {
            return $"global::{typedConstant.Value!.GetType().FullName}";
        }

        return typedConstant.Type!.GloballyQualified();
    }

    public static string GetRawTypedConstantValue(TypedConstant typedConstant)
    {
        // Use the formatter for consistent handling
        return _formatter.FormatForCode(typedConstant);
    }

    private static string FormatPrimitive(TypedConstant typedConstant)
    {
        return FormatPrimitive(typedConstant.Value);
    }

    public static string FormatPrimitive(object? value)
    {
        switch (value)
        {
            case string s:
                return SymbolDisplay.FormatLiteral(s, quote: true);
            case char c:
                return SymbolDisplay.FormatLiteral(c, quote: true);
            case bool b:
                return b ? "true" : "false";
            case float.NaN:
                return "float.NaN";
            case float f when float.IsPositiveInfinity(f):
                return "float.PositiveInfinity";
            case float f when float.IsNegativeInfinity(f):
                return "float.NegativeInfinity";
            case double.NaN:
                return "double.NaN";
            case double d when double.IsPositiveInfinity(d):
                return "double.PositiveInfinity";
            case double d when double.IsNegativeInfinity(d):
                return "double.NegativeInfinity";
            case null:
                return "null";
            default:
                return value.ToString() ?? "null";
        }
    }
}
