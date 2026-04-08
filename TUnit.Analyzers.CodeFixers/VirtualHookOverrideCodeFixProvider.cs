using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VirtualHookOverrideCodeFixProvider)), Shared]
public class VirtualHookOverrideCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove redundant hook attribute";

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.RedundantHookAttributeOnOverride.Id);

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
            var attributeSyntax = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .FirstAncestorOrSelf<AttributeSyntax>();

            if (attributeSyntax is null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => RemoveAttributeAsync(context.Document, attributeSyntax, c),
                    equivalenceKey: Title),
                diagnostic);
        }
    }

    private static async Task<Document> RemoveAttributeAsync(Document document, AttributeSyntax attributeSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var attributeList = (AttributeListSyntax)attributeSyntax.Parent!;

        SyntaxNode newRoot;
        if (attributeList.Attributes.Count == 1)
        {
            // KeepNoTrivia so we don't leave a stray blank line where the attribute list was.
            newRoot = root.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia)!;
        }
        else
        {
            var newAttributeList = attributeList.WithAttributes(
                attributeList.Attributes.Remove(attributeSyntax));
            newRoot = root.ReplaceNode(attributeList, newAttributeList);
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
