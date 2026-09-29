using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Generated code roots non-public test methods with [DynamicDependency], which .NET Framework and .NET Standard
/// lack. A .NET Standard test library can still be trimmed as part of a .NET 5+ app, so
/// <see cref="DynamicDependencyPolyfillGenerator"/> declares the attribute when nothing else provides it.
/// </summary>
internal class DynamicDependencyPolyfillGeneratorTests
{
    // The shape generated code uses: the attribute on a no-op lambda passed as an argument.
    private const string DynamicDependencyUsage =
        """
        namespace MyTests;

        internal class Tests
        {
            internal void Test(int value) { }

            internal static System.Action Root() =>
                [System.Diagnostics.CodeAnalysis.DynamicDependency("Test(System.Int32)", typeof(Tests))] static () => { };
        }
        """;

    private const string UserDeclaredAttribute =
        """
        namespace System.Diagnostics.CodeAnalysis
        {
            internal sealed class DynamicDependencyAttribute : Attribute
            {
                public DynamicDependencyAttribute(string memberSignature, Type type) { }
            }
        }
        """;

    [Test]
    public async Task Declares_attribute_when_compilation_lacks_it()
    {
        var generated = RunGenerator(CreateCompilation(references: []), buildProperties: null);

        await Assert.That(generated).HasSingleItem();
        await Assert.That(generated[0]).Contains("internal sealed class DynamicDependencyAttribute");
    }

    [Test]
    [Arguments("EnableTUnitPolyfills", "false")]
    [Arguments("EnableTUnitSourceGeneration", "false")]
    [Arguments("PolySharpIncludeGeneratedTypes", "")]
    public async Task Skipped_when_opted_out_or_PolySharp_provides_it(string property, string value)
    {
        var generated = RunGenerator(
            CreateCompilation(references: []),
            new Dictionary<string, string> { [$"build_property.{property}"] = value });

        await Assert.That(generated).IsEmpty();
    }

    [Test]
    public async Task Skipped_when_project_declares_attribute()
    {
        var generated = RunGenerator(CreateCompilation(references: [], UserDeclaredAttribute), buildProperties: null);

        await Assert.That(generated).IsEmpty();
    }

    [Test]
    public async Task Generated_usage_compiles_on_current_target_framework()
    {
        // On .NET Framework the attribute is missing, so the generator must supply it; on .NET it exists and a
        // second declaration would be ambiguous. Either way the generated usage must compile.
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "DynamicDependencyPolyfill",
            [CSharpSyntaxTree.ParseText(DynamicDependencyUsage, parseOptions)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new DynamicDependencyPolyfillGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString());

        await Assert.That(errors).IsEmpty();
    }

    private static CSharpCompilation CreateCompilation(MetadataReference[] references, params string[] sources)
    {
        return CSharpCompilation.Create(
            "DynamicDependencyPolyfill",
            sources.Select(s => CSharpSyntaxTree.ParseText(s)),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static string[] RunGenerator(Compilation compilation, Dictionary<string, string>? buildProperties)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new DynamicDependencyPolyfillGenerator());

        if (buildProperties != null)
        {
            driver = driver.WithUpdatedAnalyzerConfigOptions(
                new TestAnalyzerConfigOptionsProvider(buildProperties.ToImmutableDictionary()));
        }

        return driver.RunGenerators(compilation)
            .GetRunResult()
            .GeneratedTrees
            .Select(t => t.GetText().ToString())
            .ToArray();
    }
}
