using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator.Discovery;

/// <summary>
/// Per-compilation memo for the discovery transforms.
/// <para>
/// Every <c>Mock.Of&lt;T&gt;()</c> / <c>T.Mock()</c> call site is transformed on its own, and each
/// transform used to walk the full member surface and the transitive interface closure of its
/// target. A type mocked at a hundred call sites was modelled a hundred times per compilation, and
/// the transforms run again for every site on every edit. The models only depend on the target
/// symbol, the mocking mode and the consuming compilation (member accessibility and
/// InternalsVisibleTo via <see cref="Compilation.Assembly"/>, namespace conflicts via the
/// compilation's own declarations), so within one compilation the first call site's result is
/// reused by all others.
/// </para>
/// <para>
/// The cache is keyed on the <see cref="Compilation"/> instance and never outlives it, so there is
/// no cross-compilation state to invalidate: a new compilation (any edit) starts empty.
/// </para>
/// <para>
/// Values are computed before <c>GetOrAdd</c> runs, so two transforms that miss on the same key
/// at the same time can both build a model and one of them is discarded. The computation is pure,
/// so the outcome is the same either way; downstream consumers rely on value equality, not on
/// reference identity. A cancelled computation throws before storing, so it is never memoized.
/// </para>
/// </summary>
internal sealed class MockDiscoveryCache
{
    private static readonly ConditionalWeakTable<Compilation, MockDiscoveryCache> Caches = new();
    private static readonly ConditionalWeakTable<Compilation, MockDiscoveryCache>.CreateValueCallback Create = static _ => new MockDiscoveryCache();

    private int _mayReferenceGeneratedStaticExtensions = -1;

    private MockDiscoveryCache()
    {
    }

    public static MockDiscoveryCache For(Compilation compilation) => Caches.GetValue(compilation, Create);

    /// <summary>Results of <c>BuildSingleTypeModel</c>; <see langword="null"/> means "not mockable".</summary>
    public ConcurrentDictionary<SingleTypeKey, MockTypeModel?> SingleTypeModels { get; } = new();

    /// <summary>A single-type model followed by its transitive auto-mock interface models.</summary>
    public ConcurrentDictionary<SingleTypeKey, ImmutableArray<MockTypeModel>> ModelsWithTransitiveDependencies { get; } = new();

    /// <summary>All models produced by one <c>Mock.Of&lt;T1, T2, ...&gt;()</c> combination.</summary>
    public ConcurrentDictionary<TypeListKey, ImmutableArray<MockTypeModel>> MultiTypeModels { get; } = new();

    /// <summary>
    /// Whether the compilation can see any type named <c>*_MockStaticExtension</c>, in any
    /// namespace. A generator never sees its own output, so such a type can only be declared in
    /// source or come from a referenced assembly; when there is none, a <c>T.Mock()</c> call
    /// cannot already bind to one and the per-site binding check can be skipped.
    /// </summary>
    public bool MayReferenceGeneratedStaticExtensions(Compilation compilation)
    {
        var value = Volatile.Read(ref _mayReferenceGeneratedStaticExtensions);
        if (value < 0)
        {
            value = ScanForStaticExtensions(compilation) ? 1 : 0;
            Volatile.Write(ref _mayReferenceGeneratedStaticExtensions, value);
        }

        return value == 1;
    }

    private static bool ScanForStaticExtensions(Compilation compilation)
    {
        // Source declarations: answered from the declaration table, no symbols are created.
        if (compilation.ContainsSymbolsWithName(IsStaticExtensionName, SymbolFilter.Type))
        {
            return true;
        }

        // Referenced assemblies: an extension producing TUnit.Mocks mocks has to reference
        // TUnit.Mocks, so only those assemblies (a handful) are walked, across every namespace.
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (ReferencesTUnitMocks(assembly) && ContainsStaticExtension(assembly.GlobalNamespace))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStaticExtensionName(string name)
        => name.EndsWith("_MockStaticExtension", System.StringComparison.Ordinal);

    private static bool ReferencesTUnitMocks(IAssemblySymbol assembly)
    {
        if (assembly.Name == "TUnit.Mocks")
        {
            return true;
        }

        foreach (var module in assembly.Modules)
        {
            foreach (var reference in module.ReferencedAssemblies)
            {
                if (reference.Name == "TUnit.Mocks")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsStaticExtension(INamespaceSymbol ns)
    {
        // Extension classes are top-level static classes, so nested types need no visit.
        foreach (var type in ns.GetTypeMembers())
        {
            if (IsStaticExtensionName(type.Name))
            {
                return true;
            }
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            if (ContainsStaticExtension(child))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>Memo key for a single mocked type in one mocking mode.</summary>
internal readonly struct SingleTypeKey : System.IEquatable<SingleTypeKey>
{
    public SingleTypeKey(INamedTypeSymbol type, bool isPartialMock, bool isWrapMock)
    {
        Type = type;
        IsPartialMock = isPartialMock;
        IsWrapMock = isWrapMock;
    }

    public INamedTypeSymbol Type { get; }
    public bool IsPartialMock { get; }
    public bool IsWrapMock { get; }

    public bool Equals(SingleTypeKey other)
        => IsPartialMock == other.IsPartialMock
           && IsWrapMock == other.IsWrapMock
           && SymbolEqualityComparer.IncludeNullability.Equals(Type, other.Type);

    public override bool Equals(object? obj) => obj is SingleTypeKey other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = SymbolEqualityComparer.IncludeNullability.GetHashCode(Type);
            hash = hash * 31 + (IsPartialMock ? 1 : 0);
            hash = hash * 31 + (IsWrapMock ? 1 : 0);
            return hash;
        }
    }
}

/// <summary>Memo key for an ordered list of type arguments.</summary>
internal readonly struct TypeListKey : System.IEquatable<TypeListKey>
{
    public TypeListKey(ImmutableArray<ITypeSymbol> types) => Types = types;

    public ImmutableArray<ITypeSymbol> Types { get; }

    public bool Equals(TypeListKey other)
    {
        if (Types.Length != other.Types.Length)
        {
            return false;
        }

        for (var i = 0; i < Types.Length; i++)
        {
            if (!SymbolEqualityComparer.IncludeNullability.Equals(Types[i], other.Types[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is TypeListKey other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var type in Types)
            {
                hash = hash * 31 + SymbolEqualityComparer.IncludeNullability.GetHashCode(type);
            }

            return hash;
        }
    }
}
