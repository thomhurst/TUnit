using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator;

public static class SyntaxExtensions
{
    public static ISymbol? GetSymbolInfo(this SyntaxNode syntaxNode, SemanticModel semanticModel)
    {
        if (semanticModel.SyntaxTree != syntaxNode.SyntaxTree)
        {
            semanticModel = semanticModel.Compilation.GetSemanticModel(syntaxNode.SyntaxTree);
        }
        
        return semanticModel.GetSymbolInfo(syntaxNode).Symbol;
    }
}