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

    private const string PublicAttribute =
        """
        namespace System.Runtime.CompilerServices
        {
            public sealed class ModuleInitializerAttribute : Attribute
            {
            }
        }
        """;

    // What Polyfill emits with PolyUseEmbeddedAttribute: the compiler ignores [Embedded] types from other
    // assemblies during lookup, even when InternalsVisibleTo makes them accessible.
    private const string EmbeddedAttributeVisibleToTests =
        """
        [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ModuleInitializerPolyfill")]

        namespace Microsoft.CodeAnalysis
        {
            [Embedded]
            internal sealed class EmbeddedAttribute : System.Attribute
            {
            }
        }

        namespace System.Runtime.CompilerServices
        {
            [Microsoft.CodeAnalysis.Embedded]
            internal sealed class ModuleInitializerAttribute : Attribute
            {
            }
        }
        """;

    [Test]
    public async Task Declares_attribute_when_compilation_lacks_it()
    {
        // No references at all, so the attribute is definitely missing on every test target framework.
        var generated = RunGenerator(CreateCompilation(references: []), buildProperties: null);

        await Assert.That(generated).HasSingleItem();
        await Verify(generated);
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
        var errors = CompileWithGenerator(ReferencesHelper.References);

        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task Module_initializer_compiles_when_several_references_declare_public_attribute()
    {
        // Several accessible definitions from references are ambiguous (CS0433), including the core library's
        // on .NET, so the generator must declare its own, which takes precedence over the referenced ones.
        MetadataReference[] references =
        [
            ..ReferencesHelper.References,
            CreateAttributeReference("PolyfillOne", PublicAttribute),
            CreateAttributeReference("PolyfillTwo", PublicAttribute),
        ];

        var errors = CompileWithGenerator(references);

        await Assert.That(errors).IsEmpty();
    }

#if NET
    // The Roslyn version the .NET Framework target runs against predates user-declared EmbeddedAttribute (CS8336).
    [Test]
    public async Task Ignores_embedded_attribute_from_reference_visible_through_InternalsVisibleTo()
    {
        // A test project for a library that uses Polyfill sees the library's internal, [Embedded] polyfill through
        // InternalsVisibleTo. The compiler ignores it, so on .NET the core library's attribute is the only usable
        // one and declaring our own would break Polyfill's [TypeForwardedTo] for the attribute (CS0729).
        MetadataReference[] references =
        [
            ..ReferencesHelper.References,
            CreateAttributeReference("PolyfilledLibrary", EmbeddedAttributeVisibleToTests),
        ];

        var generated = RunGenerator(CreateCompilation(references, ModuleInitializerUsage), buildProperties: null);

        await Assert.That(generated).IsEmpty();
        await Assert.That(CompileWithGenerator(references)).IsEmpty();
    }
#endif

    private static string[] CompileWithGenerator(IEnumerable<MetadataReference> references)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "ModuleInitializerPolyfill",
            [CSharpSyntaxTree.ParseText(ModuleInitializerUsage, parseOptions)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ModuleInitializerPolyfillGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        return output.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();
    }

    private static MetadataReference CreateAttributeReference(string assemblyName, string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);

        if (!result.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
        }

        return MetadataReference.CreateFromImage(stream.ToArray());
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
