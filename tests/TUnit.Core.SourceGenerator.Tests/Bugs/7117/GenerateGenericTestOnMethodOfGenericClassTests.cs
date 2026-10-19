using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests.Bugs._7117;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/7117.
///
/// <c>[GenerateGenericTest]</c> on a generic method of a generic class whose type arguments come from
/// a class-level data source (not a class-level <c>[GenerateGenericTest]</c>) used to emit generated
/// code that referenced the open method type parameter, which fails with CS0246. The snapshot
/// harness only inspects generator diagnostics, so this test compiles the generated output.
/// </summary>
internal class GenerateGenericTestOnMethodOfGenericClassTests
{
    private const string Source =
        """
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.Linq;
        global using global::System.Threading.Tasks;
        global using global::TUnit.Core;

        namespace MyTests;

        [Arguments(1)]
        public class GenericClass<T>(T value)
        {
            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments(1)]
            public void Run<TMethod>(TMethod arg) { }

            [Test]
            [GenerateGenericTest(typeof(string))]
            public void NoMethodRows<TMethod>() { }

            [Test]
            [GenerateGenericTest(typeof(long))]
            [Arguments(1)]
            public void UsesBoth<TMethod>(T classArg) { }
        }

        [Arguments("a")]
        [Arguments(2)]
        public class TwoClassRows<T>(T value)
        {
            [Test]
            [GenerateGenericTest(typeof(int))]
            [GenerateGenericTest(typeof(string))]
            [Arguments(1)]
            public void Run<TMethod>(TMethod arg) { }
        }

        [GenerateGenericTest(typeof(string))]
        public class ClassGenerateGenericTest<T>
        {
            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments(1)]
            public void Run<TMethod>(TMethod arg) { }
        }

        [Arguments(1)]
        public class ClassRowMethodDataSource<T>(T value)
        {
            public static IEnumerable<Func<(string, int)>> Rows() { yield return () => ("a", 1); }

            [Test]
            [MethodDataSource(nameof(Rows))]
            public void FromMethodDataSource<TMethod>(TMethod arg, int count) { }
        }

        [GenerateGenericTest(typeof(long))]
        public class ClassGenerateGenericTestMethodDataSource<T>
        {
            public static IEnumerable<Func<(string, int)>> Rows() { yield return () => ("a", 1); }

            [Test]
            [MethodDataSource(nameof(Rows))]
            public void FromMethodDataSource<TMethod>(TMethod arg, int count) { }
        }

        public class MethodRowInfersClass<T>
        {
            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments("x", 1)]
            public void Run<TMethod>(T classValue, TMethod arg) { }
        }
        """;

    [Test]
    public async Task Generated_code_compiles()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);

        var compilation = CSharpCompilation.Create(
                "Issue7117",
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

        var generated = string.Join(
            Environment.NewLine + "----" + Environment.NewLine,
            output.SyntaxTrees.Skip(1).Select(t => t.GetText().ToString()));

        await Assert.That(errors)
            .IsEmpty()
            .Because($"generated code must compile. Errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine, errors.Select(e => e.ToString()))
                + Environment.NewLine + generated);

        // Both the class and the method are closed over their own type arguments.
        await Assert.That(generated).Contains(".Run<int>(");
        await Assert.That(generated).Contains(".NoMethodRows<string>(");
        await Assert.That(generated).Contains(".UsesBoth<long>(");
        await Assert.That(generated).Contains(".FromMethodDataSource<string>(");
        await Assert.That(generated).DoesNotContain("<TMethod>");
    }
}
