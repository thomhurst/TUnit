using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using TUnit.AspNetCore.Analyzers;

namespace TUnit.AspNetCore.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseTestWebApplicationFactoryCodeFixProvider)), Shared]
public class UseTestWebApplicationFactoryCodeFixProvider : CodeFixProvider
{
    private const string Title = "Inherit from TestWebApplicationFactory<T>";
    private const string TestWebApplicationFactoryName = "TestWebApplicationFactory";
    private const string TestWebApplicationFactoryNamespace = "TUnit.AspNetCore";

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.DirectWebApplicationFactoryInheritance.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var baseTypeSyntax = root
                .FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .FirstAncestorOrSelf<BaseTypeSyntax>();

            if (baseTypeSyntax is null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => ReplaceBaseTypeAsync(context.Document, baseTypeSyntax, c),
                    equivalenceKey: Title),
                diagnostic);
        }
    }

    private static async Task<Document> ReplaceBaseTypeAsync(
        Document document,
        BaseTypeSyntax baseTypeSyntax,
        CancellationToken cancellationToken)
    {
        var genericName = baseTypeSyntax.Type switch
        {
            GenericNameSyntax g => g,
            QualifiedNameSyntax { Right: GenericNameSyntax q } => q,
            AliasQualifiedNameSyntax { Name: GenericNameSyntax a } => a,
            _ => null,
        };

        if (genericName is null)
        {
            return document;
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return document;
        }

        var newTypeName = SyntaxFactory.GenericName(SyntaxFactory.Identifier(TestWebApplicationFactoryName))
            .WithTypeArgumentList(genericName.TypeArgumentList);

        var newBaseType = baseTypeSyntax.WithType(newTypeName)
            .WithTriviaFrom(baseTypeSyntax)
            .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);

        var newCompilationUnit = compilationUnit.ReplaceNode(baseTypeSyntax, newBaseType);
        newCompilationUnit = AddUsingIfMissing(newCompilationUnit, TestWebApplicationFactoryNamespace);

        return document.WithSyntaxRoot(newCompilationUnit);
    }

    private static CompilationUnitSyntax AddUsingIfMissing(CompilationUnitSyntax compilationUnit, string namespaceName)
    {
        if (ContainsUsing(compilationUnit.Usings, namespaceName) ||
            compilationUnit.Members.Any(m => ContainsUsingInNamespace(m, namespaceName)))
        {
            return compilationUnit;
        }

        var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

        return compilationUnit.AddUsings(newUsing);
    }

    private static bool ContainsUsingInNamespace(MemberDeclarationSyntax member, string namespaceName) => member switch
    {
        BaseNamespaceDeclarationSyntax ns =>
            ContainsUsing(ns.Usings, namespaceName) ||
            ns.Members.Any(m => ContainsUsingInNamespace(m, namespaceName)),
        _ => false,
    };

    private static bool ContainsUsing(SyntaxList<UsingDirectiveSyntax> usings, string namespaceName)
    {
        foreach (var directive in usings)
        {
            if (directive.Alias is null &&
                directive.StaticKeyword.IsKind(SyntaxKind.None) &&
                directive.Name?.ToString() == namespaceName)
            {
                return true;
            }
        }

        return false;
    }
}
