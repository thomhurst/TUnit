using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitUsingDirectiveCodeFixProvider)), Shared]
public class XUnitUsingDirectiveCodeFixProvider : CodeFixProvider
{
    public override sealed ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.XunitUsingDirectives.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override sealed async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Rules.XunitUsingDirectives.Title.ToString(),
                    createChangedDocument: c => RemoveDirectivesAsync(context.Document, root?.FindNode(diagnosticSpan), c),
                    equivalenceKey: Rules.XunitUsingDirectives.Title.ToString()),
                diagnostic);
        }
    }

    private static async Task<Document> RemoveDirectivesAsync(Document document, SyntaxNode? node, CancellationToken cancellationToken)
    {
        if (node is not UsingDirectiveSyntax usingDirectiveSyntax)
        {
            return document;
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        var newNode = root.RemoveNode(usingDirectiveSyntax, SyntaxRemoveOptions.KeepNoTrivia);
        
        return newNode is not null ? document.WithSyntaxRoot(newNode!) : document;
    }
}