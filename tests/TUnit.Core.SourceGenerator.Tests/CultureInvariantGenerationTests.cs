using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Generators run inside the consumer's compiler, under whatever culture the build machine has.
/// sv-SE (also fi-FI, nb-NO, ...) formats a negative number with U+2212 MINUS SIGN, which C#
/// rejects (CS1056), and de-DE formats 2.5 as "2,5". Generated code must therefore be identical
/// under every culture. The snapshot tests run under one culture and cannot see this.
/// </summary>
internal class CultureInvariantGenerationTests
{
    private const string Source =
        """
        global using global::System;
        global using global::System.Threading.Tasks;
        global using global::TUnit.Core;
        global using static global::TUnit.Core.HookType;

        namespace MyTests;

        public class NegativeAndFractionalValues
        {
            [Before(Test, Order = -5)]
            public void SetUp() { }

            [After(Test, Order = int.MinValue + 1000)]
            public void TearDown() { }

            [Test]
            [Arguments(-1, -2.5, -3.5f, 4.25, -9L, (sbyte) -7)]
            public void Numbers(int i, double d, float f, double positive, long l, sbyte s) { }

            [Test]
            [Arguments(-1.55)]
            public void DecimalFromDouble(decimal value) { }
        }
        """;

    [Test]
    [Arguments("sv-SE")]
    [Arguments("de-DE")]
    [Arguments("ar-SA")]
    [Arguments("tr-TR")]
    public async Task Generated_code_is_identical_under_any_culture_and_compiles(string cultureName)
    {
        var invariant = Generate(CultureInfo.InvariantCulture);
        var underCulture = Generate(CultureInfo.GetCultureInfo(cultureName));

        await Assert.That(underCulture.Errors)
            .IsEmpty()
            .Because($"generated code must compile under {cultureName}:{Environment.NewLine}"
                + string.Join(Environment.NewLine, underCulture.Errors));
        const string separator = "\n----\n";
        await Assert.That(string.Join(separator, underCulture.Sources)).IsEqualTo(string.Join(separator, invariant.Sources));
    }

    private static (string[] Sources, string[] Errors) Generate(CultureInfo culture)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;

        try
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
            var compilation = CSharpCompilation.Create(
                "CultureInvariantGeneration",
                [CSharpSyntaxTree.ParseText(Source, parseOptions)],
                ReferencesHelper.References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new TestMetadataGenerator().AsSourceGenerator(), new HookMetadataGenerator().AsSourceGenerator()],
                parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

            var sources = driver.GetRunResult().GeneratedTrees
                .Select(t => t.GetText().ToString())
                .ToArray();
            var errors = output.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToArray();

            return (sources, errors);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
