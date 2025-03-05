using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitClassFixtureCodeFixProvider)), Shared]
public class XUnitClassFixtureCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.XunitClassFixtures.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Rules.XunitClassFixtures.Title.ToString(),
                    createChangedDocument: c => ConvertAttributesAsync(context.Document, root?.FindNode(diagnosticSpan), c),
                    equivalenceKey: Rules.XunitClassFixtures.Title.ToString()),
                diagnostic);
        }
    }
    
    private static async Task<Document> ConvertAttributesAsync(Document document, SyntaxNode? node, CancellationToken cancellationToken)
    {
        if (node is not SimpleBaseTypeSyntax simpleBaseTypeSyntax)
        {
            return document;
        }

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        
        var newExpression = GetNewExpression(simpleBaseTypeSyntax);
        
        if (newExpression != null)
        {
            var classDeclaration = GetClassDeclaration(simpleBaseTypeSyntax)!;

            SyntaxNode toRemove = classDeclaration.BaseList?.ChildNodes().Count() > 1
                ? simpleBaseTypeSyntax
                : classDeclaration.BaseList!;

            editor.ReplaceNode(classDeclaration,
                classDeclaration
                    .RemoveNode(toRemove, SyntaxRemoveOptions.KeepTrailingTrivia)!
                    .AddAttributeLists(
                        SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(newExpression))
                    )
            );
        }
        
        return editor.GetChangedDocument();
    }

    private static ClassDeclarationSyntax? GetClassDeclaration(SimpleBaseTypeSyntax simpleBaseTypeSyntax)
    {
        var parent = simpleBaseTypeSyntax.Parent;
        
        while (parent != null && !parent.IsKind(SyntaxKind.ClassDeclaration))
        {
            parent = parent.Parent;
        }

        return parent as ClassDeclarationSyntax;
    }

    private static AttributeSyntax? GetNewExpression(SimpleBaseTypeSyntax simpleBaseTypeSyntax)
    {
        if (simpleBaseTypeSyntax.Type is not GenericNameSyntax genericNameSyntax
            || !genericNameSyntax.TypeArgumentList.Arguments.Any())
        {
            return null;
        }

        return SyntaxFactory.Attribute(
            SyntaxFactory.GenericName(SyntaxFactory.ParseToken("ClassDataSource"), genericNameSyntax.TypeArgumentList),
            SyntaxFactory.AttributeArgumentList()
                .AddArguments(
                    SyntaxFactory.AttributeArgument(
                        nameEquals: SyntaxFactory.NameEquals("Shared"),
                        nameColon: null,
                        expression: SyntaxFactory.ParseExpression("SharedType.PerClass")
                    )
                )
        ).NormalizeWhitespace();
    }
}