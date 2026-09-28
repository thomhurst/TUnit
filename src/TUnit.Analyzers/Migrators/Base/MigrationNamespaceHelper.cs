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
    public static bool ContainsNamespace(Compilation compilation, string dottedPath, Func<INamespaceSymbol, bool> predicate)
    {
        if (ContainsNamespace(compilation.Assembly.GlobalNamespace, dottedPath, predicate))
        {
            return true;
        }

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (ContainsNamespace(assembly.GlobalNamespace, dottedPath, predicate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsNamespace(INamespaceSymbol globalNamespace, string dottedPath, Func<INamespaceSymbol, bool> predicate)
    {
        return PathExists(globalNamespace, dottedPath) || AnyNamespace(globalNamespace, predicate);
    }

    private static bool PathExists(INamespaceSymbol globalNamespace, string dottedPath)
    {
        var current = globalNamespace;

        foreach (var segment in dottedPath.Split('.'))
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
