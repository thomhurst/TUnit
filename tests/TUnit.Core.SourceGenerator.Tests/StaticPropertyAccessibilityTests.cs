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
        await Check(source, inaccessible, enabled, accessibility);
    }

    [Test]
    [Arguments(false, true)]
    [Arguments(true, true)]
    [Arguments(false, false)]
    [Arguments(true, false)]
    public async Task File_local_container(bool nested, bool enabled)
    {
        var property = "[TUnit.Core.Arguments(\"value\")] public static string Value { get; set; }";
        var source = "file class Helper { " + (nested ? "public class Nested { " + property + " }" : property) + " }";
        await Check(source, true, enabled);
    }

    [Test]
    [Arguments("class Helper<T>")]
    [Arguments("struct Helper")]
    [Arguments("record Helper")]
    [Arguments("record struct Helper")]
    public async Task Skipped_helper_has_no_diagnostic(string declaration)
    {
        var source = $$"""
            public class Outer
            {
                private {{declaration}}
                {
                    [TUnit.Core.Arguments("value")]
                    public static string Value { get; set; }
                }
            }
            """;
        await Check(source, true, true, expectDiagnostic: false);
    }

    [Test]
    public async Task Internal_leaf_in_private_container_reports_once_when_inherited()
    {
        const string source = """
            public class Outer
            {
                private class Helper
                {
                    internal class Nested
                    {
                        [TUnit.Core.Arguments("value")]
                        public static string Value { get; set; }
                    }
                    private class Derived : Nested { }
                }
            }
            """;
        await Check(source, true, true);
    }

    private static async Task Check(string source, bool inaccessible, bool enabled,
        string? snapshotParameter = null, bool expectDiagnostic = true)
    {
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
        await Assert.That(diagnostics.Length).IsEqualTo(enabled && inaccessible && expectDiagnostic ? 1 : 0);
        if (enabled && inaccessible && expectDiagnostic)
        {
            await Assert.That(diagnostics[0].Id).IsEqualTo("TUNITGEN005");
            await Assert.That(source.Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length))
                .IsEqualTo("Value");
            if (snapshotParameter is not null)
            {
                await Verify(diagnostics[0].GetMessage()).UseParameters(snapshotParameter);
            }
        }
    }
}
