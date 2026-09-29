using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.Analyzers.Helpers;

/// <summary>
/// Per-compilation cache of the TUnit.Assertions symbols the analyzers compare against.
/// </summary>
/// <remarks>
/// Every <c>Assert.That(...)</c> is inspected by several analyzers. Resolving these once per compilation
/// (at compilation start) lets them compare symbols instead of building display strings or calling
/// <c>Compilation.GetTypeByMetadataName</c> per invocation, and lets them skip registration entirely
/// when TUnit.Assertions isn't referenced.
/// </remarks>
internal sealed class AssertionSymbols
{
    private static readonly ConditionalWeakTable<Compilation, AssertionSymbols> Cache = new();

    private AssertionSymbols(Compilation compilation)
    {
        Assert = ResolveType(compilation, "TUnit.Assertions.Assert");
        ShouldExtensions = ResolveType(compilation, "TUnit.Assertions.Should.ShouldExtensions");
        IAssertionSource = ResolveType(compilation, "TUnit.Assertions.Core.IAssertionSource");
        IAssertionSourceOfT = ResolveType(compilation, "TUnit.Assertions.Core.IAssertionSource`1");
        IShouldSource = ResolveType(compilation, "TUnit.Assertions.Should.Core.IShouldSource");
        IShouldSourceOfT = ResolveType(compilation, "TUnit.Assertions.Should.Core.IShouldSource`1");
        AssertionOfT = ResolveType(compilation, "TUnit.Assertions.Core.Assertion`1");
    }

    /// <summary><c>TUnit.Assertions.Assert</c>, or null when TUnit.Assertions isn't referenced.</summary>
    public INamedTypeSymbol? Assert { get; }

    /// <summary><c>TUnit.Assertions.Should.ShouldExtensions</c>, or null when TUnit.Assertions.Should isn't referenced.</summary>
    public INamedTypeSymbol? ShouldExtensions { get; }

    public INamedTypeSymbol? IAssertionSource { get; }

    public INamedTypeSymbol? IAssertionSourceOfT { get; }

    public INamedTypeSymbol? IShouldSource { get; }

    public INamedTypeSymbol? IShouldSourceOfT { get; }

    public INamedTypeSymbol? AssertionOfT { get; }

    /// <summary>
    /// Resolves a type by metadata name from the compilation or any referenced assembly.
    /// </summary>
    /// <remarks>
    /// <c>Compilation.GetTypeByMetadataName</c> ignores references that are only reachable through an
    /// <c>extern alias</c>, and returns null when the name is defined in more than one assembly. Calls
    /// through such references still bind to these types, so fall back to probing each referenced
    /// assembly directly rather than silently disabling the analyzers.
    /// </remarks>
    public static INamedTypeSymbol? ResolveType(Compilation compilation, string metadataName)
    {
        if (compilation.GetTypeByMetadataName(metadataName) is { } type)
        {
            return type;
        }

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (assembly.GetTypeByMetadataName(metadataName) is { } referencedType)
            {
                return referencedType;
            }
        }

        return null;
    }

    public static AssertionSymbols For(Compilation compilation)
        => Cache.GetValue(compilation, static c => new AssertionSymbols(c));

    /// <summary>
    /// True for <c>TUnit.Assertions.Assert.That(...)</c> overloads.
    /// </summary>
    public bool IsAssertThat(IMethodSymbol method)
        => method.Name == "That"
           && Assert is not null
           && SymbolEqualityComparer.Default.Equals(method.ContainingType, Assert);

    /// <summary>
    /// True for the <c>value.Should()</c> entry-point overloads in <c>TUnit.Assertions.Should.ShouldExtensions</c>.
    /// </summary>
    public bool IsShould(IMethodSymbol method)
        => method.Name == "Should"
           && ShouldExtensions is not null
           && SymbolEqualityComparer.Default.Equals(method.ContainingType, ShouldExtensions);
}
