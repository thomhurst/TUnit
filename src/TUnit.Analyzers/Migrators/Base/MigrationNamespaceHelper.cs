using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Migrators.Base;

/// <summary>
/// Allocation-free namespace checks used by the migration analyzers instead of building
/// <see cref="ISymbol.ToDisplayString"/> strings for every symbol they inspect.
/// </summary>
internal static class MigrationNamespaceHelper
{
    /// <summary>
    /// Returns true when <paramref name="ns"/> is <c>root.child</c> or a namespace nested inside it,
    /// i.e. <c>ns.ToDisplayString()</c> is <c>"root.child"</c> or starts with <c>"root.child."</c>.
    /// </summary>
    public static bool IsNamespaceOrNested(INamespaceSymbol? ns, string root, string child)
    {
        for (var current = ns; current is { IsGlobalNamespace: false }; current = current.ContainingNamespace)
        {
            if (current.Name == child
                && current.ContainingNamespace is { IsGlobalNamespace: false } parent
                && parent.Name == root
                && parent.ContainingNamespace is { IsGlobalNamespace: true })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the outermost non-global namespace containing <paramref name="ns"/> (or <paramref name="ns"/> itself).
    /// </summary>
    public static INamespaceSymbol GetRootNamespace(INamespaceSymbol ns)
    {
        var current = ns;

        while (current.ContainingNamespace is { IsGlobalNamespace: false } parent)
        {
            current = parent;
        }

        return current;
    }

    /// <summary>
    /// Returns true when the namespace <paramref name="dottedPath"/> exists in the compilation's source or in any
    /// referenced assembly (including extern-aliased ones), or when any of their namespaces satisfies <paramref name="predicate"/>.
    /// </summary>
    /// <remarks>
    /// This gates the migration analyzers' semantic checks, so it must never return false when those checks could match.
    /// The <paramref name="predicate"/> walk is deliberately exhaustive (every namespace at every depth, not only roots):
    /// the semantic checks test a symbol's leaf namespace name (e.g. <c>Name.StartsWith("Xunit")</c>), which also matches
    /// nested namespaces such as <c>Foo.XunitExtras</c>. Referenced assemblies are enumerated directly rather than through
    /// <see cref="Compilation.GlobalNamespace"/>, so extern-aliased references are included too.
    /// </remarks>
    public static bool ContainsNamespace(Compilation compilation, string dottedPath, Func<INamespaceSymbol, bool> predicate)
    {
        var segments = dottedPath.Split('.');

        if (ContainsNamespace(compilation.Assembly.GlobalNamespace, segments, predicate))
        {
            return true;
        }

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (ContainsNamespace(assembly.GlobalNamespace, segments, predicate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsNamespace(INamespaceSymbol globalNamespace, string[] segments, Func<INamespaceSymbol, bool> predicate)
    {
        return PathExists(globalNamespace, segments) || AnyNamespace(globalNamespace, predicate);
    }

    private static bool PathExists(INamespaceSymbol globalNamespace, string[] segments)
    {
        var current = globalNamespace;

        foreach (var segment in segments)
        {
            INamespaceSymbol? next = null;

            foreach (var member in current.GetNamespaceMembers())
            {
                if (member.Name == segment)
                {
                    next = member;
                    break;
                }
            }

            if (next is null)
            {
                return false;
            }

            current = next;
        }

        return true;
    }

    private static bool AnyNamespace(INamespaceSymbol ns, Func<INamespaceSymbol, bool> predicate)
    {
        foreach (var member in ns.GetNamespaceMembers())
        {
            if (predicate(member) || AnyNamespace(member, predicate))
            {
                return true;
            }
        }

        return false;
    }
}
