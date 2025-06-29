using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions.Analyzers.Extensions;

namespace TUnit.Assertions.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitAssertionCodeFixProvider)), Shared]
public class XUnitAssertionCodeFixProvider : CodeFixProvider
{
    public override sealed ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.XUnitAssertion.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override sealed async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnosticNode = root?.FindNode(diagnosticSpan);

            if (diagnosticNode is not InvocationExpressionSyntax expressionSyntax)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Resources.TUnitAssertions0009Title,
                    createChangedDocument: c => ConvertAssertionAsync(context, expressionSyntax, c),
                    equivalenceKey: nameof(Resources.TUnitAssertions0009Title)),
                diagnostic);
        }
    }

    private static async Task<Document> ConvertAssertionAsync(CodeFixContext context, InvocationExpressionSyntax expressionSyntax, CancellationToken cancellationToken)
    {
        var document = context.Document;

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return document;
        }

        if (expressionSyntax.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            return document;
        }

        var expected = expressionSyntax.ArgumentList.Arguments.ElementAtOrDefault(0);
        var actual = expressionSyntax.ArgumentList.Arguments.ElementAtOrDefault(1) ?? expressionSyntax.ArgumentList.Arguments.ElementAtOrDefault(0);

        var methodName = memberAccessExpressionSyntax.Name.Identifier.ValueText;

        var genericArgs = GetGenericArguments(memberAccessExpressionSyntax.Name);

        var newExpression = await GetNewExpression(context, memberAccessExpressionSyntax, methodName, actual, expected, genericArgs, expressionSyntax.ArgumentList.Arguments);

        if (newExpression != null)
        {
            compilationUnit = compilationUnit.ReplaceNode(expressionSyntax, newExpression.WithTriviaFrom(expressionSyntax));
        }

        return document.WithSyntaxRoot(compilationUnit);
    }

    private static async Task<ExpressionSyntax?> GetNewExpression(CodeFixContext context,
        MemberAccessExpressionSyntax memberAccessExpressionSyntax, string method,
        ArgumentSyntax? actual, ArgumentSyntax? expected, string genericArgs,
        SeparatedSyntaxList<ArgumentSyntax> argumentListArguments)
    {
        var isGeneric = !string.IsNullOrEmpty(genericArgs);

        return method switch
        {
            "Equal" => await IsEqualTo(context, argumentListArguments, actual, expected),

            "NotEqual" => await IsNotEqualTo(context, argumentListArguments, actual, expected),

            "Contains" => await Contains(context, memberAccessExpressionSyntax, actual, expected),

            "DoesNotContain" => SyntaxFactory.ParseExpression($"Assert.That({actual}).DoesNotContain({expected})"),

            "StartsWith" => SyntaxFactory.ParseExpression($"Assert.That({actual}).StartsWith({expected})"),

            "EndsWith" => SyntaxFactory.ParseExpression($"Assert.That({actual}).EndsWith({expected})"),

            "NotNull" => SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotNull()"),

            "Null" => SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNull()"),

            "True" => SyntaxFactory.ParseExpression($"Assert.That({actual}).IsTrue()"),

            "False" => SyntaxFactory.ParseExpression($"Assert.That({actual}).IsFalse()"),

            "Same" => SyntaxFactory.ParseExpression($"Assert.That({actual}).IsSameReferenceAs({expected})"),

            "NotSame" => SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotSameReferenceAs({expected})"),

            "IsAssignableTo" => isGeneric
                ? SyntaxFactory.ParseExpression($"Assert.That({actual}).IsAssignableTo<{genericArgs}>()")
                : SyntaxFactory.ParseExpression($"Assert.That({actual}).IsAssignableTo({expected})"),

            "IsNotAssignableTo" => isGeneric
                ? SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotAssignableTo<{genericArgs}>()")
                : SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotAssignableTo({expected})"),

            "IsAssignableFrom" => isGeneric
                ? SyntaxFactory.ParseExpression($"Assert.That({actual}).IsAssignableFrom<{genericArgs}>()")
                : SyntaxFactory.ParseExpression($"Assert.That({actual}).IsAssignableFrom({expected})"),

            "IsNotAssignableFrom" => isGeneric
                ? SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotAssignableFrom<{genericArgs}>()")
                : SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotAssignableFrom({expected})"),

            "All" => SyntaxFactory.ParseExpression($"Assert.That({actual}).All().Satisfy({expected})"),

            "Single" => SyntaxFactory.ParseExpression($"Assert.That({actual}).HasSingleItem()"),

            "IsType" => isGeneric
                ? SyntaxFactory.ParseExpression($"Assert.That({actual}).IsTypeOf<{genericArgs}>()")
                : SyntaxFactory.ParseExpression($"Assert.That({actual}).IsTypeOf({expected})"),

            "IsNotType" => isGeneric
                ? SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotTypeOf<{genericArgs}>()")
                : SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotTypeOf({expected})"),

            "Empty" => SyntaxFactory.ParseExpression($"Assert.That({actual}).IsEmpty()"),

            "NotEmpty" => SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotEmpty()"),

            "Fail" => SyntaxFactory.ParseExpression("Fail.Test()"),

            "Skip" => SyntaxFactory.ParseExpression("Skip.Test()"),

            "Throws" or "ThrowsAsync" => isGeneric
                ? SyntaxFactory.ParseExpression($"Assert.That({actual}).ThrowsExactly<{genericArgs}>()")
                : SyntaxFactory.ParseExpression($"Assert.That({actual}).ThrowsExactly({expected})"),

            "ThrowsAny" or "ThrowsAnyAsync" => isGeneric
                ? SyntaxFactory.ParseExpression($"Assert.That({actual}).Throws<{genericArgs}>()")
                : SyntaxFactory.ParseExpression($"Assert.That({actual}).Throws({expected})"),

            _ => null
        };
    }

    private static async Task<ExpressionSyntax> IsNotEqualTo(CodeFixContext context,
        SeparatedSyntaxList<ArgumentSyntax> argumentListArguments,
        ArgumentSyntax? actual, ArgumentSyntax? expected)
    {
        if (argumentListArguments.Count >= 3 && argumentListArguments[2].Expression is LiteralExpressionSyntax literalExpressionSyntax
                                             && decimal.TryParse(literalExpressionSyntax.Token.ValueText, out _))
        {
            return SyntaxFactory.ParseExpression(
                $"Assert.That({actual}).IsNotEqualTo({expected}).Within({literalExpressionSyntax})");
        }

        var semanticModel = await context.Document.GetSemanticModelAsync();

        var actualSymbol = semanticModel.GetSymbolInfo(actual!.Expression).Symbol;
        var expectedSymbol = semanticModel.GetSymbolInfo(expected!.Expression).Symbol;

        if (actualSymbol is not null && expectedSymbol is not null
                                     && GetType(actualSymbol) is { } actualSymbolType
                                     && GetType(expectedSymbol) is { } expectedSymbolType
                                     && IsEnumerable(actualSymbolType)
                                     && IsEnumerable(expectedSymbolType))
        {
            return SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotEquivalentTo({expected})");
        }

        return SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotEqualTo({expected})");
    }

    private static async Task<ExpressionSyntax> IsEqualTo(CodeFixContext context,
        SeparatedSyntaxList<ArgumentSyntax> argumentListArguments,
        ArgumentSyntax? actual, ArgumentSyntax? expected)
    {
        if (argumentListArguments.Count >= 3 && argumentListArguments[2].Expression is LiteralExpressionSyntax literalExpressionSyntax
            && decimal.TryParse(literalExpressionSyntax.Token.ValueText, out _))
        {
            return SyntaxFactory.ParseExpression(
                $"Assert.That({actual}).IsEqualTo({expected}).Within({literalExpressionSyntax})");
        }

        var semanticModel = await context.Document.GetSemanticModelAsync();

        var actualSymbol = semanticModel.GetSymbolInfo(actual!.Expression).Symbol;
        var expectedSymbol = semanticModel.GetSymbolInfo(expected!.Expression).Symbol;

        if (actualSymbol is not null && expectedSymbol is not null
                                     && GetType(actualSymbol) is { } actualSymbolType
                                     && GetType(expectedSymbol) is { } expectedSymbolType
                                     && IsEnumerable(actualSymbolType)
                                     && IsEnumerable(expectedSymbolType))
        {
            return SyntaxFactory.ParseExpression($"Assert.That({actual}).IsEquivalentTo({expected})");
        }

        return SyntaxFactory.ParseExpression($"Assert.That({actual}).IsEqualTo({expected})");
    }

    private static ITypeSymbol? GetType(ISymbol symbol)
    {
        if (symbol is ITypeSymbol typeSymbol)
        {
            return typeSymbol;
        }

        if (symbol is IPropertySymbol propertySymbol)
        {
            return propertySymbol.Type;
        }

        if (symbol is IFieldSymbol fieldSymbol)
        {
            return fieldSymbol.Type;
        }

        if (symbol is ILocalSymbol localSymbol)
        {
            return localSymbol.Type;
        }

        return null;
    }

    private static bool IsEnumerable(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol)
        {
            return true;
        }

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol.GloballyQualifiedNonGeneric() is "global::System.Collections.IEnumerable" or "global::System.Collections.Generic.IEnumerable")
        {
            return true;
        }

        return typeSymbol.AllInterfaces.Any(i => i.GloballyQualified() == "global::System.Collections.IEnumerable");
    }

    private static async Task<ExpressionSyntax> Contains(CodeFixContext context,
        MemberAccessExpressionSyntax memberAccessExpressionSyntax, ArgumentSyntax? actual, ArgumentSyntax? expected)
    {
        var semanticModel = await context.Document.GetSemanticModelAsync();

        var symbol = semanticModel.GetSymbolInfo(memberAccessExpressionSyntax).Symbol;

        if (symbol is IMethodSymbol { Parameters.Length: 2 } methodSymbol &&
            methodSymbol.Parameters[0].Type.Name == "IEnumerable" && methodSymbol.Parameters[1].Type.Name == "Predicate")
        {
            // Swap them - This overload is the other way around to the other ones.
            (actual, expected) = (expected, actual);
        }

        return SyntaxFactory.ParseExpression($"Assert.That({actual}).Contains({expected})");
    }

    public static string GetGenericArguments(ExpressionSyntax expressionSyntax)
    {
        if (expressionSyntax is GenericNameSyntax genericName)
        {
            return string.Join(", ", genericName.TypeArgumentList.Arguments.ToList());
        }

        return string.Empty;
    }
}
