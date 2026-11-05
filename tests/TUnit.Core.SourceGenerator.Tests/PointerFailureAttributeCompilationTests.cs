using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class PointerFailureAttributeCompilationTests
{
    [Test]
    public async Task Pointer_attributes_on_generic_failures_compile()
    {
        const string source = """
            using System;
            using TUnit.Core;
            using System.Threading.Tasks;

            [GenerateGenericTest(typeof(int*))]
            [GenerateGenericTest(typeof(int))]
            public unsafe class PointerClass<T>
            {
                [Test]
                public void NonGenericMethod() { }

                [Test]
                [GenerateGenericTest(typeof(string))]
                public Task GenericMethod<TMethod>() => Task.CompletedTask;
            }
            """;
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "PointerFailureAttributes",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMetadataGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        await Assert.That(errors).IsEmpty()
            .Because(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
    }
}
