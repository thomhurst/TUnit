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

        if (symbol.IsConst(out var constantValue))
        {
            return Literal(constantValue);
        }
        
        return SyntaxFactory
            .IdentifierName(symbol!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
            .WithoutTrivia();
    }

    private static SyntaxNode Literal(object? constantValue)
    {
        return constantValue switch
        {
            null => SyntaxFactory.LiteralExpression(SyntaxKind.NullKeyword),
            string strValue => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(strValue)),
            char charValue => SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(charValue)),
            bool boolValue => boolValue ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression) 
                : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression),
            int intValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(intValue)),
            double doubleValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(doubleValue)),
            float floatValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(floatValue)),
            long longValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(longValue)),
            decimal decimalValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(decimalValue)),
            uint uintValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(uintValue)),
            ulong ulongValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(ulongValue)),
            ushort ushortValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(ushortValue)),
            _ => throw new ArgumentOutOfRangeException(nameof(constantValue), constantValue, $"Unknown constant type: {constantValue?.GetType()}")
        };
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
        
        if (childNodes.Length == 2
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