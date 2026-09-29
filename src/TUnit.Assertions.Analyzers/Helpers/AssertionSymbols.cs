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
/// when TUnit.Assertions isn't referenced. Each entry holds every matching symbol (see <see cref="TypeSymbolSet"/>),
/// so calls bound through a second, extern-aliased copy of TUnit.Assertions are still recognized.
/// </remarks>
internal sealed class AssertionSymbols
{
    private static readonly ConditionalWeakTable<Compilation, AssertionSymbols> Cache = new();

    private AssertionSymbols(Compilation compilation)
    {
        var hasExternAliasedReferences = TypeSymbolSet.HasExternAliasedReferences(compilation);

        Assert = TypeSymbolSet.Resolve(compilation, "TUnit.Assertions.Assert", hasExternAliasedReferences);
        ShouldExtensions = TypeSymbolSet.Resolve(compilation, "TUnit.Assertions.Should.ShouldExtensions", hasExternAliasedReferences);
        IAssertionSource = TypeSymbolSet.Resolve(compilation, "TUnit.Assertions.Core.IAssertionSource", hasExternAliasedReferences);
        IAssertionSourceOfT = TypeSymbolSet.Resolve(compilation, "TUnit.Assertions.Core.IAssertionSource`1", hasExternAliasedReferences);
        IShouldSource = TypeSymbolSet.Resolve(compilation, "TUnit.Assertions.Should.Core.IShouldSource", hasExternAliasedReferences);
        IShouldSourceOfT = TypeSymbolSet.Resolve(compilation, "TUnit.Assertions.Should.Core.IShouldSource`1", hasExternAliasedReferences);
        AssertionOfT = TypeSymbolSet.Resolve(compilation, "TUnit.Assertions.Core.Assertion`1", hasExternAliasedReferences);
    }

    /// <summary><c>TUnit.Assertions.Assert</c>; empty when TUnit.Assertions isn't referenced.</summary>
    public TypeSymbolSet Assert { get; }

    /// <summary><c>TUnit.Assertions.Should.ShouldExtensions</c>; empty when TUnit.Assertions.Should isn't referenced.</summary>
    public TypeSymbolSet ShouldExtensions { get; }

    public TypeSymbolSet IAssertionSource { get; }

    public TypeSymbolSet IAssertionSourceOfT { get; }

    public TypeSymbolSet IShouldSource { get; }

    public TypeSymbolSet IShouldSourceOfT { get; }

    public TypeSymbolSet AssertionOfT { get; }

    public static AssertionSymbols For(Compilation compilation)
        => Cache.GetValue(compilation, static c => new AssertionSymbols(c));

    /// <summary>
    /// True for <c>TUnit.Assertions.Assert.That(...)</c> overloads.
    /// </summary>
    public bool IsAssertThat(IMethodSymbol method)
        => method.Name == "That" && Assert.Contains(method.ContainingType);

    /// <summary>
    /// True for the <c>value.Should()</c> entry-point overloads in <c>TUnit.Assertions.Should.ShouldExtensions</c>.
    /// </summary>
    public bool IsShould(IMethodSymbol method)
        => method.Name == "Should" && ShouldExtensions.Contains(method.ContainingType);
}
