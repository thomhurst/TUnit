using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Assertions.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitAssertionCodeFixProvider)), Shared]
public class XUnitAssertionCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.XUnitAssertion.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
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
                    createChangedDocument: c => ConvertAssertionAsync(context.Document, expressionSyntax, c),
                    equivalenceKey: nameof(Resources.TUnitAssertions0009Title)),
                diagnostic);
        }
    }
    
    private static async Task<Document> ConvertAssertionAsync(Document document, InvocationExpressionSyntax expressionSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var compilationUnit = root as CompilationUnitSyntax;
        
        if (compilationUnit is null)
        {
            return document;
        }

        var newRoot = compilationUnit.AddUsings(
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("TUnit.Assertions")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("TUnit.Assertions.Extensions"))
        );

        var newExpression = GetNewExpression(expressionSyntax);
        
        if (newExpression != null)
        {
            newRoot = newRoot.ReplaceNode(expressionSyntax, newExpression.WithTriviaFrom(expressionSyntax));
        }

        newRoot = TryRemoveUsingStatement(newRoot);

        return document.WithSyntaxRoot(newRoot);
    }

    private static CompilationUnitSyntax TryRemoveUsingStatement(CompilationUnitSyntax root)
    {
        var usingsToRemove = root.Usings
            .Where(u => u.Name?.ToString().StartsWith("Xunit") is true)
            .ToList();

        if (!usingsToRemove.Any())
        {
            return root;
        }

        return root.RemoveNodes(usingsToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;
    }

    private static ExpressionSyntax? GetNewExpression(InvocationExpressionSyntax expressionSyntax)
    {
        var methodName = (expressionSyntax.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.ValueText;
        var actual = expressionSyntax.ArgumentList.Arguments.ElementAtOrDefault(0);
        var expected = expressionSyntax.ArgumentList.Arguments.ElementAtOrDefault(1) ?? actual;

        return methodName switch
        {
            "Equal" => SyntaxFactory.ParseExpression($"Assert.That({actual}).IsEqualTo({expected})"),
            "NotEqual" => SyntaxFactory.ParseExpression($"Assert.That({actual}).IsNotEqualTo({expected})"),
            // Add other cases as needed
            _ => null
        };
    }
}