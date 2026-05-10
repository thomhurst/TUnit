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
    internal static bool HasConflict(Compilation compilation, INamedTypeSymbol target, bool hasEvents)
    {
        var nsName = target.ContainingNamespace?.ToDisplayString() ?? "";
        var ns = ResolveNamespaceInCompilation(compilation, nsName);
        if (ns is null)
        {
            return false;
        }

        var typeName = target.Name;
        foreach (var existing in ns.GetTypeMembers())
        {
            if (IsCollidingName(existing.Name, typeName, hasEvents))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsCollidingName(string existingName, string typeName, bool hasEvents)
    {
        if (existingName.Length <= typeName.Length
            || !existingName.StartsWith(typeName, System.StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = existingName.AsSpan(typeName.Length);
        return suffix.SequenceEqual("Mock".AsSpan())
            || suffix.SequenceEqual("Mockable".AsSpan())
            || suffix.SequenceEqual("MockFactory".AsSpan())
            || suffix.SequenceEqual("_MockStaticExtension".AsSpan())
            || suffix.SequenceEqual("_MockMemberExtensions".AsSpan())
            || (hasEvents
                && (suffix.SequenceEqual("_MockEvents".AsSpan())
                    || suffix.SequenceEqual("_MockEventsExtensions".AsSpan())));
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
            INamespaceSymbol? next = null;
            foreach (var child in current!.GetNamespaceMembers())
            {
                if (child.Name == part)
                {
                    next = child;
                    break;
                }
            }
            if (next is null)
            {
                return null;
            }
            current = next;
        }
        return current;
    }
}
