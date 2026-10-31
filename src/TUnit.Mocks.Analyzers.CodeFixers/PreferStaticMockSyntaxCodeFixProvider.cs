using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace TUnit.Mocks.Analyzers.CodeFixers;

/// <summary>
/// Rewrites <c>Mock.Of&lt;T&gt;()</c> to <c>T.Mock()</c> and <c>Mock.Of&lt;T&gt;(behavior)</c> to
/// <c>T.Mock(behavior)</c>, adding <c>using TUnit.Mocks;</c> when the generated extension
/// members aren't already in scope.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferStaticMockSyntaxCodeFixProvider)), Shared]
public class PreferStaticMockSyntaxCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use T.Mock()";
    private const string MocksNamespace = "TUnit.Mocks";

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.PreferStaticMockSyntax);

    public sealed override FixAllProvider GetFixAllProvider() =>
        FixAllProvider.Create(static async (fixAllContext, document, diagnostics) =>
            await FixDocumentAsync(document, diagnostics, fixAllContext.CancellationToken).ConfigureAwait(false));

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
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
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (root is null || semanticModel is null)
        {
            return document;
        }

        var invocations = new List<InvocationExpressionSyntax>();
        var needsUsing = false;

        foreach (var diagnostic in diagnostics)
        {
            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                    .FirstAncestorOrSelf<InvocationExpressionSyntax>() is not { } invocation
                || CreateReplacement(invocation) is null)
            {
                continue;
            }

            invocations.Add(invocation);
            needsUsing |= !IsMocksNamespaceInScope(semanticModel, invocation.SpanStart);
        }

        if (invocations.Count == 0)
        {
            return document;
        }

        var newRoot = root.ReplaceNodes(
            invocations,
            static (original, rewritten) => CreateReplacement(rewritten) ?? rewritten);

        if (needsUsing && newRoot is CompilationUnitSyntax compilationUnit)
        {
            newRoot = AddMocksUsing(compilationUnit);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private static ExpressionSyntax? CreateReplacement(InvocationExpressionSyntax invocation)
    {
        var ofName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name as GenericNameSyntax,
            GenericNameSyntax genericName => genericName,
            _ => null,
        };

        if (ofName is not { TypeArgumentList.Arguments: [var typeArgument] })
        {
            return null;
        }

        if (typeArgument is NullableTypeSyntax nullable)
        {
            typeArgument = nullable.ElementType;
        }

        if (typeArgument is not (NameSyntax or PredefinedTypeSyntax))
        {
            return null;
        }

        // Reparse so a qualified type name becomes the member-access chain it is in expression
        // position (e.g. QualifiedName `App.IFoo` -> SimpleMemberAccessExpression).
        var receiver = SyntaxFactory.ParseExpression(typeArgument.WithoutTrivia().ToFullString());

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    receiver,
                    SyntaxFactory.IdentifierName("Mock")),
                invocation.ArgumentList)
            .WithTriviaFrom(invocation);
    }

    /// <summary>
    /// The generated <c>T.Mock()</c> extension lives in the <c>TUnit.Mocks</c> namespace, so it only
    /// binds where that namespace is imported (or encloses the call). An alias such as
    /// <c>using Mock = TUnit.Mocks.Mock;</c> doesn't import extension members.
    /// </summary>
    private static bool IsMocksNamespaceInScope(SemanticModel semanticModel, int position)
    {
        for (var ns = semanticModel.GetEnclosingSymbol(position)?.ContainingNamespace; ns is { IsGlobalNamespace: false }; ns = ns.ContainingNamespace)
        {
            if (ns.ToDisplayString() == MocksNamespace)
            {
                return true;
            }
        }

        foreach (var symbol in semanticModel.LookupNamespacesAndTypes(position, name: "Mock"))
        {
            if (symbol is INamedTypeSymbol { ContainingType: null } type
                && type.ContainingNamespace.ToDisplayString() == MocksNamespace)
            {
                return true;
            }
        }

        return false;
    }

    private static CompilationUnitSyntax AddMocksUsing(CompilationUnitSyntax compilationUnit)
    {
        if (compilationUnit.Usings.Any(static u => u.Alias is null
                                                   && u.StaticKeyword.IsKind(SyntaxKind.None)
                                                   && u.Name?.ToString() == MocksNamespace))
        {
            return compilationUnit;
        }

        var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(MocksNamespace))
            .WithAdditionalAnnotations(Formatter.Annotation);

        return compilationUnit.AddUsings(usingDirective);
    }
}
