using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public sealed class FullyQualifiedWithGlobalPrefixRewriter(SemanticModel semanticModel) : CSharpSyntaxRewriter
{
    
    public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var symbol = node.GetSymbolInfo(semanticModel);

        return SyntaxFactory
            .IdentifierName(symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
            .WithoutTrivia();
    }

    public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node)
    {
        var symbol = node.GetSymbolInfo(semanticModel);
        
        return SyntaxFactory
            .IdentifierName(symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
            .WithoutTrivia();
    }
    
    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {
        var symbol = node.GetSymbolInfo(semanticModel);

        return SyntaxFactory
            .IdentifierName(symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
            .WithoutTrivia();
    }

    public override SyntaxNode VisitTypeOfExpression(TypeOfExpressionSyntax node)
    {
        var symbol = node.Type.GetSymbolInfo(semanticModel);

        return SyntaxFactory
            .TypeOfExpression(
                SyntaxFactory.ParseTypeName(
                    symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
            )
            .WithoutTrivia();
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var childNodes = node.ChildNodes().ToArray();
        
        if (childNodes.Count() == 2
            && childNodes[0].IsKind(SyntaxKind.IdentifierName)
            && ((IdentifierNameSyntax)childNodes[0]).Identifier.ValueText == "nameof"
            && childNodes[1].IsKind(SyntaxKind.ArgumentList))
        {
            // nameof() syntax
            var argumentList = (ArgumentListSyntax) childNodes[1];
            var innerIdentifierNameSyntax = (IdentifierNameSyntax)argumentList.Arguments[0].Expression;

            return SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(innerIdentifierNameSyntax!.Identifier.ValueText)
            );
        }
        
        return base.VisitInvocationExpression(node);
    }
}