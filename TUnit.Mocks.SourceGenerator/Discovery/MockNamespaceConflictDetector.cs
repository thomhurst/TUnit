using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Mocks.SourceGenerator.Discovery;

/// <summary>
/// Detects whether placing a generated mock alongside its target type would produce a
/// name collision with an existing user-declared type in that namespace. Run once per
/// type at transform time; result lives on <see cref="Models.MockTypeModel"/> as
/// <c>UseFallbackNamespace</c>.
/// </summary>
/// <remarks>
/// Resolves the target's namespace by name in the consumer's <see cref="Compilation"/>,
/// rather than walking <c>target.ContainingNamespace</c> directly. This matters when
/// <paramref name="target"/> comes from a referenced assembly: the namespace symbol on
/// the target only exposes types from that assembly, but the same namespace name may
/// have additional types declared in the consumer's compilation.
/// </remarks>
internal static class MockNamespaceConflictDetector
{
    public static bool HasConflict(Compilation compilation, INamedTypeSymbol target, bool hasEvents)
    {
        var nsName = target.ContainingNamespace?.ToDisplayString() ?? "";
        var ns = ResolveNamespaceInCompilation(compilation, nsName);
        if (ns is null)
        {
            return false;
        }

        var typeName = target.Name;

        var collidingNames = new HashSet<string>(System.StringComparer.Ordinal)
        {
            $"{typeName}Mock",
            $"{typeName}Mockable",
            $"{typeName}MockFactory",
            $"{typeName}_MockStaticExtension",
            $"{typeName}_MockMemberExtensions",
        };
        if (hasEvents)
        {
            collidingNames.Add($"{typeName}_MockEvents");
            collidingNames.Add($"{typeName}_MockEventsExtensions");
        }

        foreach (var existing in ns.GetTypeMembers())
        {
            if (collidingNames.Contains(existing.Name))
            {
                return true;
            }
        }
        return false;
    }

    private static INamespaceSymbol? ResolveNamespaceInCompilation(Compilation compilation, string namespaceName)
    {
        if (string.IsNullOrEmpty(namespaceName) || namespaceName == "<global namespace>")
        {
            return compilation.GlobalNamespace;
        }

        INamespaceSymbol? current = compilation.GlobalNamespace;
        foreach (var part in namespaceName.Split('.'))
        {
            current = current!.GetNamespaceMembers().FirstOrDefault(n => n.Name == part);
            if (current is null)
            {
                return null;
            }
        }
        return current;
    }
}
