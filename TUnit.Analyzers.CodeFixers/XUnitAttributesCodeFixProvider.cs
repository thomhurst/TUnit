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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitAttributesCodeFixProvider)), Shared]
public class XUnitAttributesCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.XunitAttributes.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Rules.XunitAttributes.Title.ToString(),
                    createChangedDocument: c => ConvertAttributesAsync(context.Document, root?.FindNode(diagnosticSpan), c),
                    equivalenceKey: Rules.XunitAttributes.Title.ToString()),
                diagnostic);
        }
    }
    
    private static async Task<Document> ConvertAttributesAsync(Document document, SyntaxNode? node, CancellationToken cancellationToken)
    {
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }

        if (node is not AttributeSyntax attributeSyntax)
        {
            return document;
        }

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        
        var newExpression = GetNewExpression(attributeSyntax);
        
        if (newExpression != null)
        {
            editor.ReplaceNode(attributeSyntax, newExpression.WithTriviaFrom(attributeSyntax));    
        }
        
        return editor.GetChangedDocument();
    }

    private static AttributeSyntax? GetNewExpression(AttributeSyntax attributeSyntax)
    {
        var name = attributeSyntax.Name.ToString();

        return name switch
        {
            "FactAttribute" => SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test")),
            "TheoryAttribute" => SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test")),
            "TraitAttribute" => SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Property"), attributeSyntax.ArgumentList),
            "InlineDataAttribute" => SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Arguments"), attributeSyntax.ArgumentList),
            "MemberDataAttribute" => SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("MethodDataSource"), attributeSyntax.ArgumentList),
            "ClassDataAttribute" => SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("MethodDataSource"), (attributeSyntax.ArgumentList ?? SyntaxFactory.AttributeArgumentList()).AddArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.IdentifierName("GetEnumerator")))),
            _ => null
        };
    }
}