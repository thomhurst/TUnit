using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MatrixDataSourceCodeFixProvider)), Shared]
public class MatrixDataSourceCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add [MatrixDataSource]";

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.MatrixDataSourceAttributeRequired.Id);

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
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var target = node.FirstAncestorOrSelf<SyntaxNode>(n => n is MethodDeclarationSyntax or TypeDeclarationSyntax);

            if (target is null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => AddMatrixDataSourceAsync(context.Document, target, c),
                    equivalenceKey: Title),
                diagnostic);
        }
    }

    private static async Task<Document> AddMatrixDataSourceAsync(Document document, SyntaxNode target, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var attributeList = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("MatrixDataSource"))));

        SyntaxNode updated = target switch
        {
            MethodDeclarationSyntax method => method.AddAttributeLists(attributeList),
            TypeDeclarationSyntax type => type.AddAttributeLists(attributeList),
            _ => throw new InvalidOperationException($"Unexpected node kind: {target.Kind()}"),
        };

        editor.ReplaceNode(target, updated);
        return editor.GetChangedDocument();
    }
}
