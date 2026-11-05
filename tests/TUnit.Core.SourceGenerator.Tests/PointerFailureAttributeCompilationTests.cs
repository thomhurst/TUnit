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
    [Test]
    public async Task Ignored_pointer_attributes_do_not_require_an_unsafe_factory()
    {
        const string source = """
            using System;
            using TUnit.Core;

            public sealed class UnrelatedAttribute(Type value) : Attribute;

            [Unrelated(typeof(int*))]
            [GenerateGenericTest(typeof(string))]
            public unsafe class InvalidClass<T> where T : struct
            {
                [Test]
                public void Run() { }
            }
            """;
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("IgnoredPointerAttributes",
            [CSharpSyntaxTree.ParseText(source, parseOptions)], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMetadataGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        var generated = string.Join(Environment.NewLine, driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString()));
        await Assert.That(generated).DoesNotContain("unsafe");
    }

    [Test]
    public async Task Private_filter_attributes_on_generic_failures_compile()
    {
        const string source = """
            using TUnit.Core;
            public class Container
            {
                private sealed class PrivateCategoryAttribute() : CategoryAttribute("PrivateCategory");

                [PrivateCategory]
                [GenerateGenericTest(typeof(string))]
                public class InvalidClass<T> where T : struct
                {
                    [Test]
                    public void Run() { }
                }
            }
            """;
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("PrivateFailureAttributes",
            [CSharpSyntaxTree.ParseText(source, parseOptions)], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMetadataGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        await Assert.That(errors).IsEmpty()
            .Because(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
    }
}
