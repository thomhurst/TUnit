using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace TUnit.Assertions.Analyzers.CodeFixers;

/// <summary>
/// A sample code fix provider that renames classes with the company name in their definition.
/// All code fixes must  be linked to specific analyzers.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AwaitAssertionCodeFixProvider)), Shared]
public class AwaitAssertionCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.AwaitAssertion.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnosticNode = root?.FindNode(diagnosticSpan);

            if (diagnosticNode is not InvocationExpressionSyntax invocationExpressionSyntax)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Resources.TUnitAssertions0002CodeFixTitle,
                    createChangedDocument: c => AwaitAssertionAsync(context.Document, invocationExpressionSyntax, c),
                    equivalenceKey: nameof(Resources.TUnitAssertions0002CodeFixTitle)),
                diagnostic);
        }
    }

    /// <summary>
    /// Executed on the quick fix action raised by the user.
    /// </summary>
    /// <param name="document">Affected source file.</param>
    /// <param name="invocationExpressionSyntax">Highlighted class declaration Syntax Node.</param>
    /// <param name="cancellationToken">Any fix is cancellable by the user, so we should support the cancellation token.</param>
    /// <returns>Clone of the solution with updates: renamed class.</returns>
    private static async Task<Document> AwaitAssertionAsync(Document document,
        InvocationExpressionSyntax invocationExpressionSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        
        var awaitExpressionSyntax = SyntaxFactory.AwaitExpression(invocationExpressionSyntax.Expression);

        editor.ReplaceNode(invocationExpressionSyntax.Expression, awaitExpressionSyntax);

        var methodBody = GetMethodBody(invocationExpressionSyntax);

        if (methodBody is not null)
        {
            CheckIfMethodIsAsync(editor, methodBody);
        }
        
        return editor.GetChangedDocument();
    }

    private static void CheckIfMethodIsAsync(DocumentEditor editor, MethodDeclarationSyntax methodBody)
    {
        var methodModifiers = methodBody.Modifiers;

        if (methodModifiers.Any(SyntaxKind.AsyncKeyword))
        {
            return;
        }

        var asyncToken = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
        
        var newModifiers = methodModifiers.Add(asyncToken);
        
        var newMethodBody = methodBody.WithModifiers(newModifiers);

        var returnType = methodBody.ReturnType;

        if (returnType is GenericNameSyntax { Identifier.Text: "Task" or "ValueTask" })
        {
            editor.ReplaceNode(methodBody, newMethodBody);
            return;
        }

        if (returnType is not IdentifierNameSyntax identifierNameSyntax ||
            (identifierNameSyntax.Identifier.Text != "Task" && identifierNameSyntax.Identifier.Text != "ValueTask"))
        {
            TypeSyntax newReturnType =
                returnType is PredefinedTypeSyntax { Keyword.Text: "void" }
                    ? SyntaxFactory.IdentifierName("Task")
                    : SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("Task"),
                        SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(returnType)));

            newMethodBody = newMethodBody.WithReturnType(newReturnType);
        }

        editor.ReplaceNode(methodBody, newMethodBody);
    }

    private static MethodDeclarationSyntax? GetMethodBody(InvocationExpressionSyntax invocationExpressionSyntax)
    {
        SyntaxNode syntax = invocationExpressionSyntax;
        
        while (syntax != null)
        {
            if(syntax is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                return methodDeclarationSyntax;
            }
            
            syntax = syntax.Parent;
        }

        return null;
    }
}