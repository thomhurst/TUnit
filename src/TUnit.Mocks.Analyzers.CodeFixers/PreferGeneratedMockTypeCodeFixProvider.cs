using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Mocks.Analyzers.CodeFixers;

/// <summary>
/// Changes a <c>Mock&lt;T&gt;</c> declaration to the generated mock type that <c>T.Mock()</c>
/// returns, e.g. <c>Mock&lt;IFoo&gt; mock = IFoo.Mock();</c> to <c>IFooMock mock = IFoo.Mock();</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferGeneratedMockTypeCodeFixProvider)), Shared]
public class PreferGeneratedMockTypeCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use the generated mock type";

    // A const, so it is baked into this assembly rather than bound at runtime (see DiagnosticIds).
    private const string MockTypeProperty = PreferGeneratedMockTypeAnalyzer.MockTypeProperty;

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.PreferGeneratedMockType);

    public sealed override FixAllProvider GetFixAllProvider() =>
        FixAllProvider.Create(static async (fixAllContext, document, diagnostics) =>
            await FixDocumentAsync(document, diagnostics, fixAllContext.CancellationToken).ConfigureAwait(false));

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(MockTypeProperty, out var mockType) || string.IsNullOrEmpty(mockType))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Declare as '{mockType}'",
                    createChangedDocument: c => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), c),
                    equivalenceKey: Title),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixDocumentAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var replacements = new Dictionary<TypeSyntax, TypeSyntax>();

        foreach (var diagnostic in diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(MockTypeProperty, out var mockType)
                || string.IsNullOrEmpty(mockType)
                || root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not TypeSyntax declaredType)
            {
                continue;
            }

            // Keep a nullable annotation: Mock<IFoo>? -> IFooMock?
            var target = declaredType is NullableTypeSyntax nullable ? nullable.ElementType : declaredType;
            replacements[target] = SyntaxFactory.ParseTypeName(mockType!).WithTriviaFrom(target);
        }

        if (replacements.Count == 0)
        {
            return document;
        }

        var newRoot = root.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]);
        return document.WithSyntaxRoot(newRoot);
    }
}
