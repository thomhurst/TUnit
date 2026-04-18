using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Assertions.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CollectionIsEqualToCodeFixProvider)), Shared]
public class CollectionIsEqualToCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.CollectionIsEqualToUsesReferenceEquality.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            // Analyzer reports a span covering `IsEqualTo(...)`; FindNode returns the enclosing invocation.
            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                    is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax identifier } })
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Resources.TUnitAssertions0016CodeFixTitle,
                    createChangedDocument: c => ReplaceAsync(context.Document, identifier, c),
                    equivalenceKey: nameof(Resources.TUnitAssertions0016CodeFixTitle)),
                diagnostic);
        }
    }

    private static async Task<Document> ReplaceAsync(Document document, IdentifierNameSyntax identifier, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var replacement = SyntaxFactory
            .IdentifierName("IsEquivalentTo")
            .WithTriviaFrom(identifier);

        return document.WithSyntaxRoot(root.ReplaceNode(identifier, replacement));
    }
}
