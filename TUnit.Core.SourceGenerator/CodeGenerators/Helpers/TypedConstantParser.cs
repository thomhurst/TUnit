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
        // Special case for decimal parameters: always use the original source text to preserve full precision
        // This is crucial because numeric literals without 'm' suffix are treated as doubles by the compiler,
        // which lose precision beyond ~15-17 digits. By using the original source text, we preserve
        // all the digits the user wrote.
        if (parameterType?.SpecialType == SpecialType.System_Decimal && element.a != null)
        {
            // Get the original source text directly from the syntax node
            var originalText = element.a.Expression.ToString();
            
            // Null safety check
            if (string.IsNullOrEmpty(originalText))
            {
                return "0m";
            }
            
            // Check if it's a numeric literal (not an identifier or expression)
            // We detect numeric literals by checking if they match a numeric pattern
            var isNumericLiteral = System.Text.RegularExpressions.Regex.IsMatch(originalText, @"^-?\d+(\.\d+)?([eE][+-]?\d+)?[dDfFmMlLuU]?$");
            
            if (isNumericLiteral)
            {
                // Remove any existing suffix and add 'm' for decimal
                var withoutSuffix = System.Text.RegularExpressions.Regex.Replace(originalText, @"[dDfFmMlLuU]+$", "");
                return withoutSuffix + "m";
            }
            
            // For non-literals (identifiers, field references, etc.), process normally
            var decimalArgExpression = element.a.Expression;
            var decimalNewExpression = decimalArgExpression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(semanticModel))!;
            return decimalNewExpression.ToString();
        }
        
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
