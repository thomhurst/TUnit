using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.Analyzers.Extensions;

public static class NamespaceExtensions
{
    /// <summary>
    /// Equivalent to <c>ns?.ToDisplayString().StartsWith("TUnit.Assertions")</c> without building the display string:
    /// true when the namespace is nested in <c>TUnit</c> and its second segment starts with <c>Assertions</c>
    /// (e.g. <c>TUnit.Assertions</c>, <c>TUnit.Assertions.Core</c>).
    /// </summary>
    public static bool IsInTUnitAssertionsNamespace(this INamespaceSymbol? ns)
    {
        for (var current = ns; current is { IsGlobalNamespace: false }; current = current.ContainingNamespace)
        {
            if (current.ContainingNamespace is { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true })
            {
                return current.Name.StartsWith("Assertions", StringComparison.Ordinal);
            }
        }

        return false;
    }
}
