using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

        // Special handling for double/float special constants (NaN, PositiveInfinity, NegativeInfinity)
        if (symbol is IFieldSymbol fieldSymbol && fieldSymbol.IsConst)
        {
            var containingType = fieldSymbol.ContainingType;
            if (containingType?.SpecialType is SpecialType.System_Double or SpecialType.System_Single)
            {
                // Get the constant value and use the helper to create the appropriate syntax
                if (fieldSymbol.HasConstantValue)
                {
                    var specialSyntax = SpecialFloatingPointValuesHelper.TryCreateSpecialFloatingPointSyntax(fieldSymbol.ConstantValue);
                    if (specialSyntax != null)
                    {
                        return specialSyntax;
                    }
                }
            }
        }

        if (node.Name is IdentifierNameSyntax identifierName)
        {
            return VisitIdentifierName(identifierName);
        }

        if (symbol is null)
        {
            return base.VisitMemberAccessExpression(node);
        }

        return SyntaxFactory
            .IdentifierName(symbol.GloballyQualified())
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
        // Check for special floating-point values first
        var specialFloatSyntax = SpecialFloatingPointValuesHelper.TryCreateSpecialFloatingPointSyntax(constantValue);
        if (specialFloatSyntax != null)
        {
            return specialFloatSyntax;
        }

        return constantValue switch
        {
            null => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression),
            string strValue => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(strValue)),
            char charValue => SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(charValue)),
            bool boolValue => boolValue ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression),
            int intValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(intValue)),
            double doubleValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(doubleValue.ToString("G17", CultureInfo.InvariantCulture) + "d", doubleValue)),
            float floatValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(floatValue.ToString("G9", CultureInfo.InvariantCulture) + "f", floatValue)),
            long longValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(longValue)),
            decimal decimalValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(decimalValue.ToString("G29", CultureInfo.InvariantCulture) + "m", decimalValue)),
            uint uintValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(uintValue + "U", uintValue)),
            ulong ulongValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(ulongValue + "UL", ulongValue)),
            ushort ushortValue => SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("ushort"), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(ushortValue))),
            byte byteValue => SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("byte"), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(byteValue))),
            sbyte sbyteValue => SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("sbyte"), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(sbyteValue))),
            short shortValue => SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("short"), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(shortValue))),
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
            && ((IdentifierNameSyntax) childNodes[0]).Identifier.ValueText == "nameof"
            && childNodes[1].IsKind(SyntaxKind.ArgumentList))
        {
            // nameof() syntax
            var argumentList = (ArgumentListSyntax) childNodes[1];
            var argumentExpression = argumentList.Arguments[0].Expression;

            if (argumentExpression is IdentifierNameSyntax identifierNameSyntax)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(identifierNameSyntax.Identifier.ValueText)
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
                SyntaxFactory.Literal(argumentExpression.ToString())
            );
        }

        return base.VisitInvocationExpression(node);
    }

#if ROSLYN4_7_OR_GREATER
    public override SyntaxNode? VisitCollectionExpression(CollectionExpressionSyntax node)
    {
        // Convert collection expressions to array initializers for property assignments
        // Collection expressions like [1, 2, 3] need to be converted to new object[] { 1, 2, 3 }
        // when used in property initializers to avoid compilation errors

        // Get the type info from the semantic model if available
        var typeInfo = semanticModel.GetTypeInfo(node);
        var elementType = "object";

        if (typeInfo.ConvertedType is IArrayTypeSymbol arrayTypeSymbol)
        {
            elementType = arrayTypeSymbol.ElementType.GloballyQualified();
        }
        else if (typeInfo.Type is IArrayTypeSymbol arrayTypeSymbol2)
        {
            elementType = arrayTypeSymbol2.ElementType.GloballyQualified();
        }

        // Visit and rewrite each element
        var rewrittenElements = new List<ExpressionSyntax>();
        foreach (var element in node.Elements)
        {
            if (element is ExpressionElementSyntax expressionElement)
            {
                var rewrittenExpression = Visit(expressionElement.Expression);
                rewrittenElements.Add((ExpressionSyntax)rewrittenExpression);
            }
        }

        // Create an array creation expression instead of a collection expression
        // This ensures compatibility with property initializers
        var arrayTypeSyntax = SyntaxFactory.ArrayType(
            SyntaxFactory.ParseTypeName(elementType),
            SyntaxFactory.SingletonList(
                SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.OmittedArraySizeExpression()
                    )
                )
            )
        );

        var initializer = SyntaxFactory.InitializerExpression(
            SyntaxKind.ArrayInitializerExpression,
            SyntaxFactory.SeparatedList(rewrittenElements)
        );

        // Create the array creation expression with proper spacing
        return SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Whitespace(" ")),
            arrayTypeSyntax,
            initializer
        );
    }
#endif
}
