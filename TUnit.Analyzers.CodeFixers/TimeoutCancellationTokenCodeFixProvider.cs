using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TimeoutCancellationTokenCodeFixProvider)), Shared]
public class TimeoutCancellationTokenCodeFixProvider : CodeFixProvider
{
    private const string SystemThreadingNamespace = "System.Threading";
    private const string CancellationTokenTypeName = "CancellationToken";
    private const string ParameterName = "cancellationToken";

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.MissingTimeoutCancellationTokenAttributes.Id);

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
            var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method is null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add CancellationToken parameter",
                    createChangedDocument: c => AddCancellationTokenAsync(context.Document, method, BodyMode.None, c),
                    equivalenceKey: "AddCancellationToken"),
                diagnostic);

            // Body-modifying actions only make sense when there's a block body to prepend to.
            // For expression-bodied methods we'd silently no-op, which is worse than not offering.
            if (method.Body is not null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Add CancellationToken parameter with ThrowIfCancellationRequested",
                        createChangedDocument: c => AddCancellationTokenAsync(context.Document, method, BodyMode.ThrowIfCancellationRequested, c),
                        equivalenceKey: "AddCancellationTokenWithThrow"),
                    diagnostic);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Add CancellationToken parameter as discard",
                        createChangedDocument: c => AddCancellationTokenAsync(context.Document, method, BodyMode.Discard, c),
                        equivalenceKey: "AddCancellationTokenAsDiscard"),
                    diagnostic);
            }
        }
    }

    private enum BodyMode
    {
        None,
        ThrowIfCancellationRequested,
        Discard,
    }

    private static async Task<Document> AddCancellationTokenAsync(
        Document document,
        MethodDeclarationSyntax method,
        BodyMode bodyMode,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(ParameterName))
            .WithType(SyntaxFactory.IdentifierName(CancellationTokenTypeName).WithTrailingTrivia(SyntaxFactory.Space));

        MethodDeclarationSyntax updated = method
            .WithParameterList(method.ParameterList.AddParameters(parameter));

        if (bodyMode != BodyMode.None && updated.Body is { } body)
        {
            StatementSyntax statement = bodyMode == BodyMode.ThrowIfCancellationRequested
                ? SyntaxFactory.ParseStatement($"{ParameterName}.ThrowIfCancellationRequested();")
                : SyntaxFactory.ParseStatement($"_ = {ParameterName};");

            statement = statement
                .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                .WithTrailingTrivia(SyntaxFactory.ElasticEndOfLine("\n"));

            var newStatements = body.Statements.Insert(0, statement);
            updated = updated.WithBody(body.WithStatements(newStatements));
        }

        updated = updated.WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(method, updated);

        if (newRoot is CompilationUnitSyntax compilationUnit)
        {
            newRoot = EnsureSystemThreadingUsing(compilationUnit);
        }

        var newDocument = document.WithSyntaxRoot(newRoot);
        return await Formatter.FormatAsync(newDocument, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static CompilationUnitSyntax EnsureSystemThreadingUsing(CompilationUnitSyntax compilationUnit)
    {
        foreach (var usingDirective in compilationUnit.Usings)
        {
            if (usingDirective.Name?.ToString() == SystemThreadingNamespace)
            {
                return compilationUnit;
            }
        }

        var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(SystemThreadingNamespace))
            .WithAdditionalAnnotations(Formatter.Annotation);

        // Insert in sorted position within the System.* group, or append if no System.* group exists.
        // Leaves non-System usings undisturbed to respect the file's existing organization.
        var insertAt = -1;
        for (var i = 0; i < compilationUnit.Usings.Count; i++)
        {
            var existing = compilationUnit.Usings[i].Name?.ToString();
            if (existing is null || !existing.StartsWith("System", StringComparison.Ordinal))
            {
                continue;
            }

            if (string.CompareOrdinal(existing, SystemThreadingNamespace) > 0)
            {
                insertAt = i;
                break;
            }

            insertAt = i + 1;
        }

        if (insertAt == -1)
        {
            return compilationUnit.AddUsings(newUsing);
        }

        return compilationUnit.WithUsings(compilationUnit.Usings.Insert(insertAt, newUsing));
    }
}
