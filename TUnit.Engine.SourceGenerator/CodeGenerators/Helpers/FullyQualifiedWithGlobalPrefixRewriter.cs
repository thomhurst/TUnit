using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

public sealed class FullyQualifiedWithGlobalPrefixRewriter(SemanticModel semanticModel) : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitPredefinedType(PredefinedTypeSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node);
        return SyntaxFactory.IdentifierName(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix));
    }
    
    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node);

        if (ShouldWriteSimpleName(symbol))
        {
            return base.VisitIdentifierName(node);
        }
        
        return node.WithIdentifier(SyntaxFactory.Identifier(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))).WithoutTrivia();
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
        return node.WithType(SyntaxFactory.ParseTypeName(symbol.Symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))).WithoutTrivia();
    }
}