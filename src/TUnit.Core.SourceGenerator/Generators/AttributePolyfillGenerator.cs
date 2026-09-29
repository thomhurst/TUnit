using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Declares an attribute that generated code relies on as an internal type, but only when the project has no
/// single usable declaration of it: it is missing (.NET Framework, .NET Standard) or declared by several
/// references so that it is ambiguous.
/// </summary>
/// <remarks>
/// The existence check needs the compilation, so this cannot use RegisterPostInitializationOutput.
/// Types from other source generators are invisible here, so PolySharp (the common generator-based
/// polyfill) is detected through the build properties it makes visible to the compiler instead.
/// Set <c>EnableTUnitPolyfills</c> to <c>false</c> to turn the fallback off.
/// </remarks>
public abstract class AttributePolyfillGenerator : IIncrementalGenerator
{
    /// <summary>The attribute's metadata name, e.g. <c>System.Runtime.CompilerServices.ModuleInitializerAttribute</c>.</summary>
    protected abstract string AttributeMetadataName { get; }

    protected abstract string HintName { get; }

    protected abstract string Source { get; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var metadataName = AttributeMetadataName;
        var hintName = HintName;
        var source = Source;

        var allowedProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) => IsAllowed(options.GlobalOptions, metadataName));

        var attributeMissingProvider = context.CompilationProvider
            .Select((compilation, _) => !HasAccessibleAttribute(compilation, metadataName));

        context.RegisterSourceOutput(attributeMissingProvider.Combine(allowedProvider), (spc, data) =>
        {
            if (data.Left && data.Right)
            {
                spc.AddSource(hintName, SourceText.From(source, System.Text.Encoding.UTF8));
            }
        });
    }

    private static bool IsAllowed(AnalyzerConfigOptions options, string metadataName)
    {
        if (IsFalse(options, "build_property.EnableTUnitSourceGeneration")
            || IsFalse(options, "build_property.EnableTUnitPolyfills"))
        {
            return false;
        }

        return !PolySharpProvidesAttribute(options, metadataName);
    }

    private static bool IsFalse(AnalyzerConfigOptions options, string key)
    {
        return options.TryGetValue(key, out var value)
            && string.Equals(value.Trim(), "false", StringComparison.OrdinalIgnoreCase);
    }

    // PolySharp marks its settings as compiler-visible, so the keys exist (possibly empty) whenever it is installed.
    // It generates every polyfill that is missing unless the type is filtered out by these two lists.
    private static bool PolySharpProvidesAttribute(AnalyzerConfigOptions options, string metadataName)
    {
        if (!options.TryGetValue("build_property.PolySharpIncludeGeneratedTypes", out var included))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(included) && !ListContainsAttribute(included, metadataName))
        {
            return false;
        }

        return !options.TryGetValue("build_property.PolySharpExcludeGeneratedTypes", out var excluded)
            || !ListContainsAttribute(excluded, metadataName);
    }

    private static bool ListContainsAttribute(string list, string metadataName)
    {
        foreach (var entry in list.Split([';', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(entry.Trim(), metadataName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAccessibleAttribute(Compilation compilation, string metadataName)
    {
        // GetTypeByMetadataName returns null when several references declare the type (common with
        // polyfill packages), so inspect every candidate. The compiler ignores [Embedded] types from other
        // assemblies, which is how Polyfill declares them, even when InternalsVisibleTo makes them accessible.
        INamedTypeSymbol? accessible = null;
        var accessibleCount = 0;

        foreach (var type in compilation.GetTypesByMetadataName(metadataName))
        {
            if (compilation.IsSymbolAccessibleWithin(type, compilation.Assembly)
                && !IsEmbeddedFromReference(type, compilation))
            {
                accessible = type;
                accessibleCount++;
            }
        }

        if (accessibleCount <= 1)
        {
            return accessible != null;
        }

        // Several accessible definitions are not always ambiguous (a declaration in source wins, for example).
        // Only the compiler's own lookup gives the right answer, so bind the name the generated code uses and
        // declare our own attribute only when that fails (CS0433). Ours is then in source, so it takes precedence
        // over the referenced ones.
        var tree = compilation.SyntaxTrees.FirstOrDefault();

        if (tree is null)
        {
            // Speculative binding needs a tree, and other generators may still emit code that uses the attribute.
            tree = CSharpSyntaxTree.ParseText(string.Empty);
            compilation = compilation.AddSyntaxTrees(tree);
        }

        var typeInfo = compilation.GetSemanticModel(tree).GetSpeculativeTypeInfo(
            0,
            SyntaxFactory.ParseTypeName("global::" + metadataName),
            SpeculativeBindingOption.BindAsTypeOrNamespace);

        return typeInfo.Type is { TypeKind: not TypeKind.Error };
    }

    private static bool IsEmbeddedFromReference(INamedTypeSymbol type, Compilation compilation)
    {
        if (SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly))
        {
            return false;
        }

        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass is { Name: "EmbeddedAttribute", ContainingNamespace: { Name: "CodeAnalysis", ContainingNamespace: { Name: "Microsoft", ContainingNamespace.IsGlobalNamespace: true } } })
            {
                return true;
            }
        }

        return false;
    }
}
