using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// The generated registration code uses [ModuleInitializer], which .NET Framework and .NET Standard lack.
/// <see cref="ModuleInitializerPolyfillGenerator"/> declares the attribute only when nothing else provides it.
/// </summary>
internal class ModuleInitializerPolyfillGeneratorTests
{
    private const string ModuleInitializerUsage =
        """
        namespace MyTests;

        internal static class Init
        {
            [System.Runtime.CompilerServices.ModuleInitializer]
            internal static void Run() { }
        }
        """;

    private const string UserDeclaredAttribute =
        """
        namespace System.Runtime.CompilerServices
        {
            internal sealed class ModuleInitializerAttribute : Attribute;
        }
        """;

    [Test]
    public async Task Declares_attribute_when_compilation_lacks_it()
    {
        // No references at all, so the attribute is definitely missing on every test target framework.
        var generated = RunGenerator(CreateCompilation(references: []), buildProperties: null);

        await Assert.That(generated).HasSingleItem();
        await Assert.That(generated[0]).Contains("internal sealed class ModuleInitializerAttribute");
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
    public async Task Declared_when_PolySharp_excludes_it()
    {
        var generated = RunGenerator(
            CreateCompilation(references: []),
            new Dictionary<string, string>
            {
                ["build_property.PolySharpIncludeGeneratedTypes"] = "",
                ["build_property.PolySharpExcludeGeneratedTypes"] = "System.Runtime.CompilerServices.ModuleInitializerAttribute",
            });

        await Assert.That(generated).HasSingleItem();
    }

    [Test]
    public async Task Skipped_when_project_declares_attribute()
    {
        var compilation = CreateCompilation(references: [], UserDeclaredAttribute);

        var generated = RunGenerator(compilation, buildProperties: null);

        await Assert.That(generated).IsEmpty();
    }

    [Test]
    public async Task Module_initializer_compiles_on_current_target_framework()
    {
        // On .NET Framework the runtime has no ModuleInitializerAttribute, so the generator must supply it;
        // on .NET it exists and a second declaration would be ambiguous. Either way the result must compile.
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "ModuleInitializerPolyfill",
            [CSharpSyntaxTree.ParseText(ModuleInitializerUsage, parseOptions)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ModuleInitializerPolyfillGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var errors = output.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();

        await Assert.That(errors).IsEmpty();
    }

    private static CSharpCompilation CreateCompilation(MetadataReference[] references, params string[] sources)
    {
        return CSharpCompilation.Create(
            "ModuleInitializerPolyfill",
            sources.Select(s => CSharpSyntaxTree.ParseText(s)),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static string[] RunGenerator(Compilation compilation, Dictionary<string, string>? buildProperties)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ModuleInitializerPolyfillGenerator());

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
