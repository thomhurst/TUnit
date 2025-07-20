using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InitOnlyPropertyCodeFixProvider)), Shared]
public sealed class InitOnlyPropertyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = 
        ImmutableArray.Create(InitOnlyPropertyAnalyzer.InitOnlyPropertyNotSupported.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the property declaration
        var propertyDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?.AncestorsAndSelf()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault();

        if (propertyDeclaration?.AccessorList == null)
        {
            return;
        }

        // Register code fix
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Change 'init' to 'set'",
                createChangedDocument: c => ChangeInitToSetAsync(context.Document, propertyDeclaration, c),
                equivalenceKey: "ChangeInitToSet"),
            diagnostic);
    }

    private static async Task<Document> ChangeInitToSetAsync(
        Document document,
        PropertyDeclarationSyntax propertyDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Find the init accessor
        var initAccessor = propertyDeclaration.AccessorList!.Accessors
            .FirstOrDefault(a => a.IsKind(SyntaxKind.InitAccessorDeclaration));

        if (initAccessor == null)
        {
            return document;
        }

        // Create a new set accessor
        var setAccessor = SyntaxFactory.AccessorDeclaration(
            SyntaxKind.SetAccessorDeclaration,
            initAccessor.AttributeLists,
            initAccessor.Modifiers,
            SyntaxFactory.Token(SyntaxKind.SetKeyword).WithTriviaFrom(initAccessor.Keyword),
            initAccessor.Body,
            initAccessor.ExpressionBody,
            initAccessor.SemicolonToken);

        // Replace the init accessor with set accessor
        var newAccessorList = propertyDeclaration.AccessorList.ReplaceNode(initAccessor, setAccessor);
        var newPropertyDeclaration = propertyDeclaration.WithAccessorList(newAccessorList);
        
        var newRoot = root.ReplaceNode(propertyDeclaration, newPropertyDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}