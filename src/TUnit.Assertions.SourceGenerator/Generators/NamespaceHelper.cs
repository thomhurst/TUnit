using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.SourceGenerator.Generators;

internal static class NamespaceHelper
{
    /// <summary>
    /// Returns the display name of <paramref name="namespaceSymbol"/>, or <see langword="null"/> for the global
    /// namespace. The global namespace displays as <c>&lt;global namespace&gt;</c>, which is not valid in a
    /// <c>using</c> directive or a qualified type name (#7133).
    /// </summary>
    public static string? GetNamespaceOrNull(INamespaceSymbol? namespaceSymbol)
        => namespaceSymbol is { IsGlobalNamespace: false } ? namespaceSymbol.ToDisplayString() : null;
}
