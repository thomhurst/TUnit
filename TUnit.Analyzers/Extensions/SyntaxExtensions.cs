using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

public static class SyntaxExtensions
{
    public static IOperation? GetOperation(this SyntaxNode syntaxNode, SemanticModel semanticModel)
    {
        if (semanticModel.SyntaxTree != syntaxNode.SyntaxTree)
        {
            semanticModel = semanticModel.Compilation.GetSemanticModel(syntaxNode.SyntaxTree);
        }
        
        return semanticModel.GetOperation(syntaxNode);
    }
}