using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Contains information about extern aliases used in a source file.
/// This allows the generator to properly qualify types that require alias prefixes.
/// </summary>
public sealed class ExternAliasContext
{
    /// <summary>
    /// Empty context with no extern aliases.
    /// </summary>
    public static readonly ExternAliasContext Empty = new(
        ImmutableArray<string>.Empty,
        ImmutableDictionary<string, string>.Empty
    );

    /// <summary>
    /// List of extern alias names declared in the file (e.g., "DirectDependency").
    /// </summary>
    public ImmutableArray<string> ExternAliases { get; }

    /// <summary>
    /// Mapping from namespace to the alias that should be used to qualify types in that namespace.
    /// Key: namespace (e.g., "System.IO.Abstractions")
    /// Value: alias name (e.g., "DirectDependency")
    /// </summary>
    public ImmutableDictionary<string, string> NamespaceToAlias { get; }

    public ExternAliasContext(
        ImmutableArray<string> externAliases,
        ImmutableDictionary<string, string> namespaceToAlias)
    {
        ExternAliases = externAliases;
        NamespaceToAlias = namespaceToAlias;
    }

    /// <summary>
    /// Gets the appropriate qualifier for a type symbol.
    /// Returns the alias qualifier (e.g., "DirectDependency::") if the type requires it,
    /// otherwise returns "global::".
    /// </summary>
    public string GetQualifierForType(ITypeSymbol typeSymbol)
    {
        if (NamespaceToAlias.IsEmpty)
        {
            return "global::";
        }

        // Check parent namespaces from most specific to least specific
        var currentNamespace = typeSymbol.ContainingNamespace;
        while (currentNamespace != null && !currentNamespace.IsGlobalNamespace)
        {
            var nsString = currentNamespace.ToDisplayString();
            if (NamespaceToAlias.TryGetValue(nsString, out var alias))
            {
                return $"{alias}::";
            }
            currentNamespace = currentNamespace.ContainingNamespace;
        }

        // Also check the full type name (in case it was mapped)
        var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        if (NamespaceToAlias.TryGetValue(fullTypeName, out var typeAlias))
        {
            return $"{typeAlias}::";
        }

        return "global::";
    }

    /// <summary>
    /// Returns true if this context has any extern aliases defined.
    /// </summary>
    public bool HasExternAliases => !ExternAliases.IsEmpty;
}
