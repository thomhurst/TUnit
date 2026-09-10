using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/6140.
///
/// The params/array argument binding used to emit the LINQ extension-method chain
/// <c>Enumerable.Range(...).Select(...).ToArray()</c>. Generated <c>.g.cs</c> files only
/// import <c>System.Linq</c> when the consuming project has <c>ImplicitUsings</c> enabled, so
/// projects without it failed to compile with CS1061 ("IEnumerable&lt;T&gt; does not contain a
/// definition for 'Select'").
///
/// Unlike the snapshot tests (whose harness injects a <c>global using System.Linq;</c> and only
/// inspects generator diagnostics, never the resulting C# compilation), this test compiles the
/// generated output WITHOUT <c>System.Linq</c> in scope and asserts there are no compiler errors.
/// It fails (CS1061) against the pre-fix generator and passes once the generated code uses
/// fully-qualified static <c>Enumerable.ToArray(Enumerable.Select(...))</c> calls.
/// </summary>
internal class ParamsArrayCompilationTests
{
    // A params test method whose body uses no LINQ — so the ONLY thing that could pull in
    // System.Linq is the generated argument-binding code, isolating the regression.
    private const string Source =
        """
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.Threading.Tasks;
        global using global::TUnit.Core;

        namespace MyTests;

        public enum RollingOrder { NewestFirst, NewestLast }

        public class RollingListTests
        {
            [Test]
            [Arguments(RollingOrder.NewestFirst, 2, 1)]
            [Arguments(RollingOrder.NewestLast, 1, 2)]
            public void Add(RollingOrder rollingOrder, params int[] expected)
            {
            }

            [Test]
            [Arguments("a", "b", "c")]
            public void LeadingParams(params string[] values)
            {
            }
        }
        """;

    [Test]
    public async Task Generated_params_binding_compiles_without_System_Linq_in_scope()
    {
        // Generated code (and the project) targets the preview language version.
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);

        var compilation = CSharpCompilation.Create(
                "ParamsArrayRegression",
                [CSharpSyntaxTree.ParseText(Source, parseOptions)],
                ReferencesHelper.References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMetadataGenerator().AsSourceGenerator()],
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
            .Because($"generated code must compile without 'using System.Linq'. Errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine, errors.Select(e => e.ToString()))
                + Environment.NewLine + generated);
    }
}
