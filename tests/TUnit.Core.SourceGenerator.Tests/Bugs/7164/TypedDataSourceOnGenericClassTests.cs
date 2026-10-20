using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests.Bugs._7164;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/7164.
///
/// A typed data source (<c>DataSourceGeneratorAttribute&lt;T&gt;</c>) infers the type arguments of a generic
/// method of a generic class, as on a non-generic class. Before the fix the generator emitted no test for a
/// method whose only method source was a typed data source.
/// </summary>
internal class TypedDataSourceOnGenericClassTests
{
    private const string Source =
        """
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.Linq;
        global using global::System.Threading.Tasks;
        global using global::TUnit.Core;

        namespace MyTests;

        public class IntSource : DataSourceGeneratorAttribute<int>
        {
            protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata) { yield return () => 42; }
        }

        public class IntStringSource : DataSourceGeneratorAttribute<int, string>
        {
            protected override IEnumerable<Func<(int, string)>> GenerateDataSources(DataGeneratorMetadata metadata) { yield return () => (42, "x"); }
        }

        [GenerateGenericTest(typeof(string))]
        public class ClassGenerateGenericTest<T>
        {
            [Test, IntSource]
            public void OnClassGenerateGenericTest<TMethod>(TMethod arg) { }

            [Test, IntSource, GenerateGenericTest(typeof(long))]
            public void WithMethodGenerateGenericTest<TMethod>(TMethod arg) { }

            [Test, IntStringSource]
            public void TwoTypeArguments<TFirst, TSecond>(TFirst first, TSecond second) { }
        }

        [Arguments(1)]
        public class ClassRow<T>(T value)
        {
            [Test, IntSource]
            public void OnClassRow<TMethod>(TMethod arg) { }
        }
        """;

    [Test]
    public async Task Typed_source_closes_the_method()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);

        var compilation = CSharpCompilation.Create(
                "Issue7164",
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

        await Assert.That(generated).Contains(".OnClassGenerateGenericTest<int>(");
        await Assert.That(generated).Contains(".WithMethodGenerateGenericTest<int>(");
        await Assert.That(generated).Contains(".WithMethodGenerateGenericTest<long>(");
        await Assert.That(generated).Contains(".TwoTypeArguments<int, string>(");
        await Assert.That(generated).Contains(".OnClassRow<int>(");
    }
}
