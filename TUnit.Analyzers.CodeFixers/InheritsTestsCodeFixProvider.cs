using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace TUnit.Analyzers.CodeFixers;

/// <summary>
/// A sample code fix provider that renames classes with the company name in their definition.
/// All code fixes must  be linked to specific analyzers.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InheritsTestsCodeFixProvider)), Shared]
public class InheritsTestsCodeFixProvider : CodeFixProvider
{
    public override sealed ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.DoesNotInheritTestsWarning.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override sealed async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnosticNode = root?.FindNode(diagnosticSpan);

            if (diagnosticNode is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                return;
            }
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Rules.DoesNotInheritTestsWarning.Title.ToString(),
                    createChangedDocument: c => AddInheritsTests(context.Document, classDeclarationSyntax, c),
                    equivalenceKey: Rules.DoesNotInheritTestsWarning.Title.ToString()),
                diagnostic);
        }
    }
    
    private static async Task<Document> AddInheritsTests(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(classDeclarationSyntax,
            classDeclarationSyntax.AddAttributeLists(
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.ParseName("InheritsTests")
                        )
                    )
                )
            )
        );

        return editor.GetChangedDocument();
    }
}