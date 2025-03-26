using System.Collections.Immutable;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitOutputHelperCodeFixProvider)), Shared]
public class XUnitOutputHelperCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.XunitTestOutputHelper.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var syntaxNode = root?.FindNode(diagnosticSpan);

            if (syntaxNode is InvocationExpressionSyntax)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Change Invocation",
                        createChangedDocument: c => FixAsync(context.Document, syntaxNode, c),
                        equivalenceKey: "ChangeInvocation"),
                    diagnostic);
            }
            else if (syntaxNode is AssignmentExpressionSyntax)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Remove Assignment",
                        createChangedDocument: c => FixAsync(context.Document, syntaxNode, c),
                        equivalenceKey: "RemoveAssignment"),
                    diagnostic);
            }
            else if (syntaxNode is ParameterSyntax)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Remove Parameter",
                        createChangedDocument: c => FixAsync(context.Document, syntaxNode, c),
                        equivalenceKey: "RemoveParameter"),
                    diagnostic);
            }
            else if (syntaxNode is FieldDeclarationSyntax)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Remove Field",
                        createChangedDocument: c => FixAsync(context.Document, syntaxNode, c),
                        equivalenceKey: "RemoveField"),
                    diagnostic);
            }
            else if (syntaxNode is PropertyDeclarationSyntax)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Remove Property",
                        createChangedDocument: c => FixAsync(context.Document, syntaxNode, c),
                        equivalenceKey: "RemoveProperty"),
                    diagnostic);
            }
        }
    }

    private static async Task<Document> FixAsync(Document document, SyntaxNode? node, CancellationToken cancellationToken)
    {
        if (node is FieldDeclarationSyntax 
            or PropertyDeclarationSyntax
            or ParameterSyntax
            or AssignmentExpressionSyntax)
        {
            return await RemoveNode(document, node, cancellationToken);
        }
        
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

        if (semanticModel is null || node is null)
        {
            return document;
        }

        var operation = semanticModel.GetOperation(node);
        
        if (operation is IInvocationOperation invocationOperation)
        {
            new StringBuilder().AppendLine("");
        }

        return document;
    }

    private static async Task<Document> RemoveNode(Document document, SyntaxNode? node, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        
        if (root is null || node is null)
        {
            return document;
        }

        var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.AddElasticMarker)!;
        
        return document.WithSyntaxRoot(newRoot);
    }
}