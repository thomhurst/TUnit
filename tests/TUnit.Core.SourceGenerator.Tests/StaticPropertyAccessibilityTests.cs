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

    [Test]
    public async Task File_level_options_suppress_diagnostic()
    {
        const string source = """
            public class Outer
            {
                private class Helper
                {
                    [TUnit.Core.Arguments("value")]
                    public static string Value { get; set; }
                }
            }
            """;
        await Check(source, true, true, expectDiagnostic: false, suppressDiagnostic: true);
    }

    [Test]
    [Arguments("")]
    [Arguments("shared.cs")]
    public async Task Duplicate_paths_preserve_tree_identity_and_file_options(string path)
    {
        const string source = """
            public class Outer
            {
                private class Helper
                {
                    [TUnit.Core.Arguments("value")]
                    public static string Value { get; set; }
                }
            }
            """;
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var first = CSharpSyntaxTree.ParseText(source, parseOptions, path);
        var second = CSharpSyntaxTree.ParseText(source.Replace("Outer", "Other"), parseOptions, path);
        var compilation = CSharpCompilation.Create("DuplicatePaths", [first, second], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSyntaxTreeOptionsProvider(new SuppressDiagnosticOptionsProvider(first)));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new StaticPropertyInitializationGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray()).IsEmpty();
        await Assert.That(driver.GetRunResult().Results[0].Exception).IsNull();
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("TUNITGEN005");
        await Assert.That(diagnostics[0].Location.SourceTree).IsEqualTo(second);
        await Assert.That(second.GetText().ToString(diagnostics[0].Location.SourceSpan)).IsEqualTo("Value");
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Tree_order_changes_refresh_diagnostic_identity(bool reorder)
    {
        const string source = """
            public class Outer
            {
                private class Helper
                {
                    [TUnit.Core.Arguments("value")]
                    public static string Value { get; set; }
                }
            }
            """;
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var offending = CSharpSyntaxTree.ParseText(source, parseOptions, "shared.cs");
        var unrelated = CSharpSyntaxTree.ParseText("public class Unrelated { }", parseOptions, "shared.cs");
        var compilation = CSharpCompilation.Create("TreeOrder", [offending, unrelated], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSyntaxTreeOptionsProvider(new SuppressDiagnosticOptionsProvider(unrelated)));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new StaticPropertyInitializationGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var initialDiagnostics);
        await Assert.That(initialDiagnostics.Length).IsEqualTo(1);
        await Assert.That(initialDiagnostics[0].Location.SourceTree).IsEqualTo(offending);

        // Keep the offending tree unchanged and reuse the driver so incremental caches participate.
        var inserted = CSharpSyntaxTree.ParseText("public class Inserted { }", parseOptions, "shared.cs");
        compilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(reorder
            ? [unrelated, offending]
            : [inserted, offending, unrelated]);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(driver.GetRunResult().Results[0].Exception).IsNull();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray()).IsEmpty();
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("TUNITGEN005");
        await Assert.That(diagnostics[0].Location.SourceTree).IsEqualTo(offending);
        await Assert.That(offending.GetText().ToString(diagnostics[0].Location.SourceSpan)).IsEqualTo("Value");
    }

    private static async Task Check(string source, bool inaccessible, bool enabled,
        string? snapshotParameter = null, bool expectDiagnostic = true, bool suppressDiagnostic = false)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("StaticPropertyAccessibility",
            [CSharpSyntaxTree.ParseText(source, parseOptions)], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSyntaxTreeOptionsProvider(suppressDiagnostic ? new SuppressDiagnosticOptionsProvider() : null));
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
            await Assert.That(diagnostics[0].Location.SourceTree).IsEqualTo(compilation.SyntaxTrees.Single());
            await Assert.That(source.Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length))
                .IsEqualTo("Value");
            if (snapshotParameter is not null)
            {
                await Verify(diagnostics[0].GetMessage()).UseParameters(snapshotParameter);
            }
        }
    }
    private sealed class SuppressDiagnosticOptionsProvider(SyntaxTree? suppressedTree = null) : SyntaxTreeOptionsProvider
    {
        public override GeneratedKind IsGenerated(SyntaxTree tree, CancellationToken cancellationToken) => GeneratedKind.Unknown;

        public override bool TryGetDiagnosticValue(SyntaxTree tree, string diagnosticId,
            CancellationToken cancellationToken, out ReportDiagnostic severity)
        {
            severity = ReportDiagnostic.Suppress;
            return diagnosticId == "TUNITGEN005" && (suppressedTree is null || ReferenceEquals(tree, suppressedTree));
        }

        public override bool TryGetGlobalDiagnosticValue(string diagnosticId,
            CancellationToken cancellationToken, out ReportDiagnostic severity)
        {
            severity = ReportDiagnostic.Default;
            return false;
        }
    }
}
