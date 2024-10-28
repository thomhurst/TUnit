using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TypedConstantParser
{
    public static string? GetTypedConstantValue(SemanticModel semanticModel,
        ExpressionSyntax argumentExpression, ITypeSymbol? parameterType)
    {
        var newExpression = argumentExpression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(semanticModel))!;

        if (parameterType?.TypeKind == TypeKind.Enum && 
            (newExpression.IsKind(SyntaxKind.UnaryMinusExpression) || newExpression.IsKind(SyntaxKind.UnaryPlusExpression)))
        {
            return $"({parameterType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)})({newExpression})";
        }

        if (parameterType?.SpecialType == SpecialType.System_Decimal)
        {
            return $"{newExpression.ToString().TrimEnd('d')}m";
        }

        return newExpression.ToString();
    }

    public static string GetFullyQualifiedTypeNameFromTypedConstantValue(TypedConstant typedConstant)
    {
        if (typedConstant.Kind == TypedConstantKind.Type)
        {
            var type = (INamedTypeSymbol)typedConstant.Value!;
            return type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        }

        if (typedConstant.Kind == TypedConstantKind.Enum)
        {
            return typedConstant.Type!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        }

        if (typedConstant.Kind is not TypedConstantKind.Error and not TypedConstantKind.Array)
        {
            return $"global::{typedConstant.Value!.GetType().FullName}";
        }

        return typedConstant.Type!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    }
}