using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Helpers;

/// <summary>
/// Per-compilation cache of the TUnit symbols most analyzers look up, plus memoized
/// <see cref="IsTestClass"/> results shared by every analyzer in the assembly.
/// </summary>
/// <remarks>
/// Analyzers query these for every type, method and attribute in the compilation. Resolving them once
/// avoids repeated <c>Compilation.GetTypeByMetadataName</c> lookups (which older Roslyn versions don't cache),
/// and lets a compilation without a TUnit.Core reference bail out immediately.
/// </remarks>
internal sealed class TUnitSymbols
{
    private static readonly ConditionalWeakTable<Compilation, TUnitSymbols> Cache = new();

    // Analyzer callbacks on one thread almost always belong to the same compilation, so check the last one
    // first: ConditionalWeakTable lookups take a lock on .NET Framework (i.e. inside Visual Studio).
    // Held weakly so a thread never keeps an old compilation alive.
    [ThreadStatic]
    private static WeakReference<TUnitSymbols>? _lastUsed;

    private readonly ConcurrentDictionary<INamedTypeSymbol, bool> _isTestClass = new(SymbolEqualityComparer.Default);

    private readonly ConcurrentDictionary<IAssemblySymbol, bool> _mayContainTUnitMembers = new(SymbolEqualityComparer.Default);

    private TUnitSymbols(Compilation compilation)
    {
        Compilation = compilation;
        TestAttribute = compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.Test.WithoutGlobalPrefix);
        BeforeAttribute = compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.BeforeAttribute.WithoutGlobalPrefix);
        AfterAttribute = compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.AfterAttribute.WithoutGlobalPrefix);
        BeforeEveryAttribute = compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.BeforeEveryAttribute.WithoutGlobalPrefix);
        AfterEveryAttribute = compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.AfterEveryAttribute.WithoutGlobalPrefix);
    }

    public Compilation Compilation { get; }

    public INamedTypeSymbol? TestAttribute { get; }

    public INamedTypeSymbol? BeforeAttribute { get; }

    public INamedTypeSymbol? AfterAttribute { get; }

    public INamedTypeSymbol? BeforeEveryAttribute { get; }

    public INamedTypeSymbol? AfterEveryAttribute { get; }

    public static TUnitSymbols For(Compilation compilation)
    {
        if (_lastUsed is { } lastUsedReference
            && lastUsedReference.TryGetTarget(out var lastUsed)
            && ReferenceEquals(lastUsed.Compilation, compilation))
        {
            return lastUsed;
        }

        var symbols = Cache.GetValue(compilation, static c => new TUnitSymbols(c));

        if (_lastUsed is { } reference)
        {
            reference.SetTarget(symbols);
        }
        else
        {
            _lastUsed = new WeakReference<TUnitSymbols>(symbols);
        }

        return symbols;
    }

    /// <summary>
    /// Exact-match <c>[Test]</c> check; see <see cref="Extensions.MethodExtensions.IsTestMethod"/>.
    /// </summary>
    public bool IsTestMethod(IMethodSymbol methodSymbol)
    {
        var testAttribute = TestAttribute;

        if (testAttribute is null)
        {
            return false;
        }

        foreach (var attribute in methodSymbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, testAttribute))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True when the type declares at least one <c>[Test]</c> method. Memoized per compilation.
    /// </summary>
    public bool IsTestClass(INamedTypeSymbol namedTypeSymbol)
    {
        if (TestAttribute is null)
        {
            return false;
        }

        if (_isTestClass.TryGetValue(namedTypeSymbol, out var isTestClass))
        {
            return isTestClass;
        }

        isTestClass = ComputeIsTestClass(namedTypeSymbol);
        _isTestClass.TryAdd(namedTypeSymbol, isTestClass);
        return isTestClass;
    }

    /// <summary>
    /// False when members of <paramref name="type"/> certainly carry no TUnit.Core attributes, because the type
    /// lives in a referenced assembly that neither is nor references the assembly declaring <c>[Test]</c>
    /// (e.g. BCL or third-party base classes). Lets analyzers that walk base types skip decoding their attributes.
    /// </summary>
    public bool MayContainTUnitMembers(INamedTypeSymbol type)
    {
        var assembly = type.ContainingAssembly;
        var tunitCoreAssembly = TestAttribute?.ContainingAssembly;

        if (assembly is null || tunitCoreAssembly is null)
        {
            return true;
        }

        if (_mayContainTUnitMembers.TryGetValue(assembly, out var result))
        {
            return result;
        }

        result = ComputeMayContainTUnitMembers(assembly, tunitCoreAssembly);
        _mayContainTUnitMembers.TryAdd(assembly, result);
        return result;
    }

    private bool ComputeMayContainTUnitMembers(IAssemblySymbol assembly, IAssemblySymbol tunitCoreAssembly)
    {
        if (SymbolEqualityComparer.Default.Equals(assembly, Compilation.Assembly)
            || SymbolEqualityComparer.Default.Equals(assembly, tunitCoreAssembly))
        {
            return true;
        }

        var tunitCoreName = tunitCoreAssembly.Identity.Name;

        if (assembly.Identity.Name == tunitCoreName)
        {
            return true;
        }

        foreach (var module in assembly.Modules)
        {
            foreach (var referencedAssembly in module.ReferencedAssemblies)
            {
                if (referencedAssembly.Name == tunitCoreName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool ComputeIsTestClass(INamedTypeSymbol namedTypeSymbol)
    {
        foreach (var member in namedTypeSymbol.GetMembers())
        {
            if (member is IMethodSymbol methodSymbol && IsTestMethod(methodSymbol))
            {
                return true;
            }
        }

        return false;
    }
}
