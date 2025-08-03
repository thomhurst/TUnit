using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class SyntaxExtensions
{
    public static ISymbol? GetSymbolInfo(this SyntaxNode syntaxNode, SemanticModel semanticModel)
    {
        if (semanticModel.SyntaxTree != syntaxNode.SyntaxTree)
        {
            if (!semanticModel.Compilation.ContainsSyntaxTree(syntaxNode.SyntaxTree))
            {
                return null;
            }

            semanticModel = semanticModel.Compilation.GetSemanticModel(syntaxNode.SyntaxTree);
        }

        return semanticModel.GetSymbolInfo(syntaxNode).Symbol;
    }
}
