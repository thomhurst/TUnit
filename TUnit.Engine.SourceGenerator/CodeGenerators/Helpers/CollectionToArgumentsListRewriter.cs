﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

public class CollectionToArgumentsListRewriter(SemanticModel semanticModel) : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node)
    {
        return new FullyQualifiedWithGlobalPrefixRewriter(semanticModel).VisitPredefinedType(node);
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        return new FullyQualifiedWithGlobalPrefixRewriter(semanticModel).VisitIdentifierName(node);
    }

    public override SyntaxNode VisitTypeOfExpression(TypeOfExpressionSyntax node)
    {
        return new FullyQualifiedWithGlobalPrefixRewriter(semanticModel).VisitTypeOfExpression(node);
    }

    public override SyntaxNode VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
    {
        return SyntaxFactory.ParseArgumentList(
            node.Initializer
                ?.Expressions
                .Select(x =>
                    x.Accept(new FullyQualifiedWithGlobalPrefixRewriter(semanticModel))?.ToFullString() ?? "null")
                .ToCommaSeparatedString()
            ?? string.Empty
        );
    }

    public override SyntaxNode VisitCollectionExpression(CollectionExpressionSyntax node)
    {
        return SyntaxFactory.ParseArgumentList(
            node.Elements
                .Select(x =>
                    x.Accept(new FullyQualifiedWithGlobalPrefixRewriter(semanticModel))?.ToFullString() ?? "null")
                .ToCommaSeparatedString()
        );
    }
}