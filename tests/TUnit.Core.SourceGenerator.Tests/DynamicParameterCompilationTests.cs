using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/7165.
///
/// <c>dynamic</c> test parameters, constructor parameters and data-source properties used to be rendered as <c>typeof(dynamic)</c>
/// in the generated metadata, which does not compile (CS1962). <c>dynamic</c> is <c>object</c> at runtime, so
/// the generator must render it as <c>typeof(object)</c>. The snapshot harness only checks generator
/// diagnostics, so this test compiles the generated output and asserts there are no compiler errors.
/// </summary>
internal class DynamicParameterCompilationTests
{
    private const string Source =
        """
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.Threading.Tasks;
        global using global::TUnit.Core;

        namespace MyTests;

        public class DynamicTests
        {
            [Test]
            [Arguments("x")]
            public void Takes_Dynamic(dynamic value)
            {
            }

            [Test]
            [Arguments(1, "a", "b")]
            public void Takes_Dynamic_Params(int first, params dynamic[] values)
            {
            }

            [Test]
            [MethodDataSource(nameof(Data))]
            public void Takes_Dynamic_Generic(List<dynamic> values, dynamic[] array)
            {
            }

            public static IEnumerable<Func<(List<dynamic>, dynamic[])>> Data()
            {
                yield return () => (new List<dynamic> { 1 }, new dynamic[] { "a" });
            }
        }

        [Arguments("x")]
        public class DynamicConstructorTests(dynamic value)
        {
            [MethodDataSource(nameof(PropertyData))]
            public required dynamic Property { get; init; }

            [MethodDataSource(nameof(PropertyData))]
            public static dynamic StaticProperty { get; set; } = null!;

            public static object PropertyData() => "y";

            [Test]
            public void Test()
            {
            }
        }
        """;

    [Test]
    public async Task Generated_metadata_for_dynamic_parameters_compiles()
    {
        // Generated code (and the project) targets the preview language version.
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);

        var compilation = CSharpCompilation.Create(
                "DynamicParameterRegression",
                [CSharpSyntaxTree.ParseText(Source, parseOptions)],
                ReferencesHelper.References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMetadataGenerator().AsSourceGenerator(), new PropertyInjectionSourceGenerator().AsSourceGenerator(), new StaticPropertyInitializationGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var errors = output.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        // Surface the full generated source if anything fails, so failures are diagnosable.
        var generated = string.Join(
            Environment.NewLine + "----" + Environment.NewLine,
            output.SyntaxTrees.Select(t => t.GetText().ToString()));

        await Assert.That(errors)
            .IsEmpty()
            .Because($"generated code for dynamic parameters must compile. Errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine, errors.Select(e => e.ToString()))
                + Environment.NewLine + generated);
    }
}
