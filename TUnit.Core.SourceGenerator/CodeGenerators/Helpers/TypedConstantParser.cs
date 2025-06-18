using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TypedConstantParser
{
    public static string GetTypedConstantValue(SemanticModel semanticModel,
        (TypedConstant typedConstant, AttributeArgumentSyntax a) element, ITypeSymbol? parameterType)
    {
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
            var type = (INamedTypeSymbol)typedConstant.Value!;
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
        if (typedConstant.IsNull)
        {
            return "null";
        }

        switch (typedConstant.Kind)
        {
            case TypedConstantKind.Primitive:
                return FormatPrimitive(typedConstant);
            case TypedConstantKind.Enum:
                return $"({typedConstant.Type!.GloballyQualified()})({typedConstant.Value})";
            case TypedConstantKind.Type:
                return $"typeof({((ITypeSymbol)typedConstant.Value!).GloballyQualified()})";
            case TypedConstantKind.Array:
                var elements = typedConstant.Values.Select(GetRawTypedConstantValue);
                return $"new[] {{ {string.Join(", ", elements)} }}";
            case TypedConstantKind.Error:
                return "default";
            default:
                throw new NotSupportedException($"Unsupported TypedConstantKind: {typedConstant.Kind}");
        }
    }

    private static string FormatPrimitive(TypedConstant typedConstant)
    {
        return FormatPrimitive(typedConstant.Value);
    }

    public static string FormatPrimitive(object? value)
    {
        return value switch
        {
            string s => SymbolDisplay.FormatLiteral(s, quote: true),
            char c => SymbolDisplay.FormatLiteral(c, quote: true),
            bool b => b ? "true" : "false",
            null => "null",
            _ => value.ToString() ?? "null"
        };
    }
}
