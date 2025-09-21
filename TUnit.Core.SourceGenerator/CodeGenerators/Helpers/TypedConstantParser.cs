﻿using Microsoft.CodeAnalysis;
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
            return $"{newExpression.ToString().TrimEnd('d')}m";
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

    public static string GetRawTypedConstantValue(TypedConstant typedConstant, ITypeSymbol? targetType = null)
    {
        // Use the formatter for consistent handling
        return _formatter.FormatForCode(typedConstant, targetType);
    }

    private static string FormatPrimitive(TypedConstant typedConstant)
    {
        return FormatPrimitive(typedConstant.Value);
    }

    public static string FormatPrimitive(object? value)
    {
        // Check for special floating-point values first
        var specialFloatValue = SpecialFloatingPointValuesHelper.TryFormatSpecialFloatingPointValue(value);
        if (specialFloatValue != null)
        {
            return specialFloatValue;
        }

        switch (value)
        {
            case string s:
                return SymbolDisplay.FormatLiteral(s, quote: true);
            case char c:
                return SymbolDisplay.FormatLiteral(c, quote: true);
            case bool b:
                return b ? "true" : "false";
            case null:
                return "null";
            // Use InvariantCulture for numeric types to ensure consistent formatting
            case double d:
                return d.ToString(System.Globalization.CultureInfo.InvariantCulture) + "d";
            case float f:
                return f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
            case decimal dec:
                return dec.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
            default:
                // For other numeric types, use InvariantCulture
                if (value is IFormattable formattable)
                {
                    return formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
                }
                return value.ToString() ?? "null";
        }
    }
}
