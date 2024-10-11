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
        var diagnostic = context.Diagnostics.Single();

        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var diagnosticNode = root?.FindNode(diagnosticSpan);

        if (diagnosticNode is not ExpressionStatementSyntax expressionStatementSyntax)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Resources.TUnitAssertions0002CodeFixTitle,
                createChangedDocument: c => AwaitAssertionAsync(context.Document, expressionStatementSyntax, c),
                equivalenceKey: nameof(Resources.TUnitAssertions0002CodeFixTitle)),
            diagnostic);
    }

    /// <summary>
    /// Executed on the quick fix action raised by the user.
    /// </summary>
    /// <param name="document">Affected source file.</param>
    /// <param name="expressionStatementSyntax">Highlighted class declaration Syntax Node.</param>
    /// <param name="cancellationToken">Any fix is cancellable by the user, so we should support the cancellation token.</param>
    /// <returns>Clone of the solution with updates: renamed class.</returns>
    private async Task<Document> AwaitAssertionAsync(Document document,
        ExpressionStatementSyntax expressionStatementSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        
        var awaitExpressionSyntax = SyntaxFactory.AwaitExpression(expressionStatementSyntax.Expression);

        editor.ReplaceNode(expressionStatementSyntax.Expression, awaitExpressionSyntax);
        
        return editor.GetChangedDocument();
    }
}