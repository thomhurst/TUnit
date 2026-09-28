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
    /// Whether any type named <c>*_MockStaticExtension</c> is visible in the <c>TUnit.Mocks</c>
    /// namespace of the compilation. A generator never sees its own output, so such a type can
    /// only come from a referenced assembly (or hand-written source); when there is none, a
    /// <c>T.Mock()</c> call cannot already bind to a generated extension and the per-site binding
    /// check can be skipped.
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
        var mocksNamespace = FindChildNamespace(FindChildNamespace(compilation.GlobalNamespace, "TUnit"), "Mocks");
        if (mocksNamespace is null)
        {
            return false;
        }

        foreach (var type in mocksNamespace.GetTypeMembers())
        {
            if (type.Name.EndsWith("_MockStaticExtension", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static INamespaceSymbol? FindChildNamespace(INamespaceSymbol? parent, string name)
    {
        if (parent is null)
        {
            return null;
        }

        foreach (var child in parent.GetNamespaceMembers())
        {
            if (child.Name == name)
            {
                return child;
            }
        }

        return null;
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
