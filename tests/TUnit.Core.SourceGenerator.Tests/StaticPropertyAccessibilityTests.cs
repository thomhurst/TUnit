using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class StaticPropertyAccessibilityTests
{
    [Test]
    [Arguments("private", true, true)]
    [Arguments("private", true, false)]
    [Arguments("protected", true, true)]
    [Arguments("private protected", true, true)]
    [Arguments("internal", false, true)]
    [Arguments("protected internal", false, true)]
    [Arguments("public", false, true)]
    public async Task Containing_type_accessibility(string accessibility, bool inaccessible, bool enabled)
    {
        var source = $$"""
            public class Outer
            {
                {{accessibility}} class Helper
                {
                    public class Nested
                    {
                        [TUnit.Core.Arguments("value")]
                        public static string Value { get; set; }
                    }
                }
            }
            """;
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("StaticPropertyAccessibility",
            [CSharpSyntaxTree.ParseText(source, parseOptions)], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new StaticPropertyInitializationGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(
            ImmutableDictionary<string, string>.Empty.Add("build_property.EnableTUnitSourceGeneration", enabled.ToString())));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray())
            .IsEmpty();
        await Assert.That(driver.GetRunResult().Results[0].Exception).IsNull();
        await Assert.That(driver.GetRunResult().Results[0].GeneratedSources.Length).IsEqualTo(!enabled || inaccessible ? 0 : 1);
        await Assert.That(diagnostics.Length).IsEqualTo(enabled && inaccessible ? 1 : 0);
        if (enabled && inaccessible)
        {
            await Assert.That(diagnostics[0].Id).IsEqualTo("TUNITGEN005");
            await Assert.That(diagnostics[0].Location.SourceTree!.GetText().ToString(diagnostics[0].Location.SourceSpan))
                .IsEqualTo("Value");
            await Verify(diagnostics[0].GetMessage()).UseParameters(accessibility);
        }
    }
}
