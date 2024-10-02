using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class TypedConstantParser
{
    public static string? GetTypedConstantValue(SemanticModel semanticModel, ExpressionSyntax argumentExpression, ITypeSymbol? type = null)
    {
        var newExpression = argumentExpression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(semanticModel))!;

        if (type?.TypeKind == TypeKind.Enum && !newExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
        {
            return $"({type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)})({newExpression})";
        }

        return newExpression.ToString();
    }

    private sealed class FullyQualifiedWithGlobalPrefixRewriter(SemanticModel semanticModel) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitPredefinedType(PredefinedTypeSyntax node)
        {
            var symbol = semanticModel.GetSymbolInfo(node);
            return SyntaxFactory.IdentifierName(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix));
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var symbol = semanticModel.GetSymbolInfo(node);
            if (symbol.Symbol!.Kind != SymbolKind.NamedType)
            {
                return base.VisitIdentifierName(node);
            }
            return node.WithIdentifier(SyntaxFactory.Identifier(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)));
        }

        public override SyntaxNode? VisitTypeOfExpression(TypeOfExpressionSyntax node)
        {
            var symbol = semanticModel.GetSymbolInfo(node.Type);
            return node.WithType(SyntaxFactory.ParseTypeName(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)));
        }
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