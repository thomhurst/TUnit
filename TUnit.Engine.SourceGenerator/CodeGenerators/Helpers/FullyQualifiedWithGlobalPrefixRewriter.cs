using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

public sealed class FullyQualifiedWithGlobalPrefixRewriter(SemanticModel semanticModel) : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node);

        return SyntaxFactory
            .IdentifierName(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
            .WithoutTrivia();
    }

    public override SyntaxNode? VisitPredefinedType(PredefinedTypeSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node);
        
        return SyntaxFactory
            .IdentifierName(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
            .WithoutTrivia();
    }
    
    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node);

        return SyntaxFactory
            .IdentifierName(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
            .WithoutTrivia();
    }

    private static bool ShouldWriteSimpleName(SymbolInfo symbol)
    {
        if (symbol.Symbol is IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.Type.TypeKind == TypeKind.Enum)
            {
                return true;
            }

            if (fieldSymbol.IsConst)
            {
                return false;
            }
        }
        
        return symbol.Symbol!.Kind != SymbolKind.NamedType;
    }

    public override SyntaxNode VisitTypeOfExpression(TypeOfExpressionSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node.Type);

        return SyntaxFactory
            .TypeOfExpression(
                SyntaxFactory.ParseTypeName(
                    symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
            )
            .WithoutTrivia();
    }
}