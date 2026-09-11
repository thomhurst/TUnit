using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

public class AotConverterScanTests
{
    [Test]
    public async Task FindsConversionsInNestedAndPartialDeclarations()
    {
        const string source = """
            using TUnit.Core;
            public struct MethodValue { public static implicit operator MethodValue(int value) => new(); }
            public struct ConstructorValue { public static implicit operator ConstructorValue(int value) => new(); }
            public struct NestedValue { public static implicit operator NestedValue(int value) => new(); }
            public struct PartialValue { public static implicit operator PartialValue(int value) => new(); }
            public struct BodyOnlyValue { public static implicit operator BodyOnlyValue(int value) => new(); }
            public partial class Tests
            {
                [Test]
                public partial void Partial(System.Type value);

                [Test, Arguments(1)]
                public void Test(MethodValue value)
                {
                    BodyOnlyValue local = 1;
                    void Helper() { BodyOnlyValue inner = 2; }
                    Helper();
                }
                public class Nested
                {
                    [Test, Arguments(1)]
                    public void Test(NestedValue value) { }
                }
            }
            """;
        const string secondPart = """
            using TUnit.Core;
            [Arguments(1)]
            public partial class Tests
            {
                public partial void Partial([Arguments(typeof(PartialValue))] System.Type value) { }
                public Tests(ConstructorValue value) { BodyOnlyValue local = 1; }
                public BodyOnlyValue Property => 1;
                private BodyOnlyValue field = 1;
            }
            """;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "ConverterDeclarations",
            [CSharpSyntaxTree.ParseText(source, parseOptions), CSharpSyntaxTree.ParseText(secondPart, parseOptions)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AotConverterGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();

        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        await Assert.That(generated).Contains("global::MethodValue");
        await Assert.That(generated).Contains("global::ConstructorValue");
        await Assert.That(generated).Contains("global::NestedValue");
        await Assert.That(generated).Contains("global::PartialValue");
        await Assert.That(generated).DoesNotContain("global::BodyOnlyValue");
    }
}
