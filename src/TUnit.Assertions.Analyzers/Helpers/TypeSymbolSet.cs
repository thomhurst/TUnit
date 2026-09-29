using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.Analyzers.Helpers;

/// <summary>
/// Every type symbol a compilation can bind to for one metadata name.
/// </summary>
/// <remarks>
/// A compilation can reach several assemblies that define the same type, for example a global reference
/// plus one or more <c>extern alias</c> references to another copy of TUnit.Assertions. Calls bound through
/// any of them must be recognized, so comparisons check every match. The common case is a single match,
/// which is stored without an array.
/// </remarks>
internal readonly struct TypeSymbolSet
{
    private readonly INamedTypeSymbol? _primary;
    private readonly INamedTypeSymbol[]? _others;

    private TypeSymbolSet(INamedTypeSymbol? primary, INamedTypeSymbol[]? others)
    {
        _primary = primary;
        _others = others;
    }

    /// <summary>True when the type isn't available to the compilation.</summary>
    public bool IsEmpty => _primary is null;

    /// <summary>True when <paramref name="type"/> is one of the resolved types.</summary>
    public bool Contains(ITypeSymbol? type)
    {
        if (type is null || _primary is null)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(type, _primary))
        {
            return true;
        }

        if (_others is null)
        {
            return false;
        }

        foreach (var other in _others)
        {
            if (SymbolEqualityComparer.Default.Equals(type, other))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True when <paramref name="assembly"/> defines one of the resolved types.</summary>
    public bool ContainsAssembly(IAssemblySymbol? assembly)
    {
        if (assembly is null || _primary is null)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(assembly, _primary.ContainingAssembly))
        {
            return true;
        }

        if (_others is null)
        {
            return false;
        }

        foreach (var other in _others)
        {
            if (SymbolEqualityComparer.Default.Equals(assembly, other.ContainingAssembly))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves every type named <paramref name="metadataName"/> in the compilation and its referenced assemblies.
    /// </summary>
    /// <remarks>
    /// <c>Compilation.GetTypeByMetadataName</c> ignores references that are only reachable through an
    /// <c>extern alias</c>, and returns null when the name is defined in more than one assembly. Its result is
    /// only complete when it is non-null and no reference has a non-global alias; otherwise each referenced
    /// assembly is probed.
    /// </remarks>
    public static TypeSymbolSet Resolve(Compilation compilation, string metadataName)
        => Resolve(compilation, metadataName, HasExternAliasedReferences(compilation));

    /// <inheritdoc cref="Resolve(Compilation, string)"/>
    /// <param name="hasExternAliasedReferences">The result of <see cref="HasExternAliasedReferences"/>, computed once by the caller.</param>
    public static TypeSymbolSet Resolve(Compilation compilation, string metadataName, bool hasExternAliasedReferences)
    {
        var globalType = compilation.GetTypeByMetadataName(metadataName);

        if (globalType is not null && !hasExternAliasedReferences)
        {
            return new TypeSymbolSet(globalType, null);
        }

        INamedTypeSymbol? primary = null;
        List<INamedTypeSymbol>? others = null;

        Add(globalType);
        Add(compilation.Assembly.GetTypeByMetadataName(metadataName));

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            Add(assembly.GetTypeByMetadataName(metadataName));
        }

        return new TypeSymbolSet(primary, others?.ToArray());

        void Add(INamedTypeSymbol? type)
        {
            if (type is null)
            {
                return;
            }

            if (primary is null)
            {
                primary = type;
                return;
            }

            if (SymbolEqualityComparer.Default.Equals(primary, type))
            {
                return;
            }

            others ??= [];

            foreach (var other in others)
            {
                if (SymbolEqualityComparer.Default.Equals(other, type))
                {
                    return;
                }
            }

            others.Add(type);
        }
    }

    /// <summary>True when any reference is visible through an <c>extern alias</c> other than <c>global</c>.</summary>
    public static bool HasExternAliasedReferences(Compilation compilation)
    {
        foreach (var reference in compilation.References)
        {
            foreach (var alias in reference.Properties.Aliases)
            {
                if (alias != "global")
                {
                    return true;
                }
            }
        }

        return false;
    }
}
