using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers.Extensions;

public static class CompilationExtensions
{
    public static async Task<bool> NamespaceExists(this Document document, string namespaceName, CancellationToken cancellationToken = default)
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