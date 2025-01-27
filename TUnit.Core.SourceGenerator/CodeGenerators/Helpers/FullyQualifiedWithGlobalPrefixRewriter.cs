using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public sealed class FullyQualifiedWithGlobalPrefixRewriter(SemanticModel semanticModel) : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(node.ToFullString()));
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var symbol = node.GetSymbolInfo(semanticModel);

        if (node.Name is IdentifierNameSyntax identifierName)
        {
            return VisitIdentifierName(identifierName);
        }

        if (symbol is null)
        {
            return base.VisitMemberAccessExpression(node);
        }
        
        return SyntaxFactory
            .IdentifierName(symbol!.GloballyQualified())
            .WithoutTrivia();
    }

    public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node)
    {
        var symbol = node.GetSymbolInfo(semanticModel);
        
        return SyntaxFactory
            .IdentifierName(symbol!.GloballyQualified())
            .WithoutTrivia();
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var symbol = node.GetSymbolInfo(semanticModel);

        if (TryParseConstant(symbol, out var constantValue))
        {
            return constantValue;
        }

        if (symbol is IMethodSymbol methodSymbol)
        {
            var type = methodSymbol.ReceiverType
                ?? methodSymbol.ContainingType;
            
            return SyntaxFactory
                .IdentifierName(type.GloballyQualified())
                .WithoutTrivia();
        }

        if (symbol is null)
        {
            return base.VisitIdentifierName(node);
        }

        return SyntaxFactory
            .IdentifierName(symbol.GloballyQualified())
            .WithoutTrivia();
    }
    
    

    private static bool TryParseConstant(ISymbol? symbol, [NotNullWhen(true)] out SyntaxNode? literalSyntax)
    {
        if (symbol is not IFieldSymbol
            {
                Type: INamedTypeSymbol
                {
                    TypeKind: TypeKind.Enum
                }
            }
            && symbol.IsConst(out var constantValue))
        {
            literalSyntax = Literal(constantValue);
            
            return true;
        }

        literalSyntax = null;
        return false;
    }


    private static SyntaxNode Literal(object? constantValue)
    {
        return constantValue switch
        {
            null => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression),
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
                    symbol!.GloballyQualified())
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
            var argumentExpression = argumentList.Arguments[0].Expression;
            
            if (argumentExpression is IdentifierNameSyntax identifierNameSyntax)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(identifierNameSyntax!.Identifier.ValueText)
                );
            }

            if (argumentExpression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(memberAccessExpressionSyntax.Name.Identifier.ValueText)
                );
            }

            return SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(argumentExpression!.ToString())
            );
        }
        
        return base.VisitInvocationExpression(node);
    }
}