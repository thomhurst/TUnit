using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers.Extensions;

public static class DocumentExtensions
{
    public static async Task<CompilationUnitSyntax> AddUsingDirectiveIfNotExistsAsync(this Document document,
        CompilationUnitSyntax root, string namespaceName, CancellationToken cancellationToken = default)
    {
        if (await UsingDirectiveExistsAsync(document, namespaceName, cancellationToken))
        {
            return root;
        }

        var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName).WithLeadingTrivia(SyntaxFactory.Space))
            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

        return root.AddUsings(usingDirective);
    }

    private static async Task<bool> UsingDirectiveExistsAsync(this Document document, string namespaceName, CancellationToken cancellationToken)
    {
        var compilation = await document.Project.GetCompilationAsync(cancellationToken);

        var namespaceSymbol = compilation?.GlobalNamespace.GetNamespaceMembers()
            .FirstOrDefault(ns => ns.ToDisplayString() == namespaceName);

        if (namespaceSymbol != null)
        {
            return true;
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken);

        if (root == null)
        {
            return false;
        }

        var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>();

        return usingDirectives.Any(u => u.Name?.ToString() == namespaceName);
    }
}
