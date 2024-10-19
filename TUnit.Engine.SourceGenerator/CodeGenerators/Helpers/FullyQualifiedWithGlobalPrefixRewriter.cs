using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

public sealed class FullyQualifiedWithGlobalPrefixRewriter(SemanticModel semanticModel) : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node);
        
        return SyntaxFactory.IdentifierName(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)).WithoutTrivia();
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node);
            
        if (symbol.Symbol is IFieldSymbol { IsConst: false })
        {
            return base.VisitIdentifierName(node);
        }
            
        return node.WithIdentifier(SyntaxFactory.Identifier(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))).WithoutTrivia();
    }

    public override SyntaxNode VisitTypeOfExpression(TypeOfExpressionSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node.Type);
        return node.WithType(SyntaxFactory.ParseTypeName(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))).WithoutTrivia();
    }
}