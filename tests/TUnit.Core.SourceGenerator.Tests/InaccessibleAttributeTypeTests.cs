using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;

namespace TUnit.Core.SourceGenerator.Tests;

public class InaccessibleAttributeTypeTests
{
    [Test]
    public async Task ExecutableGenericEntryCompilesWithPrivateExplicitType()
    {
        const string source = """
            using TUnit.Core;
            public class Container
            {
                private sealed class Hidden { }
                [GenerateGenericTest(typeof(Hidden))]
                public class Tests<T> where T : struct
                {
                    [Test, Arguments(1)]
                    public void Run(T value) { }
                }
            }
            """;
        var options = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("PrivateGenericEntry",
            [CSharpSyntaxTree.ParseText(source, options)], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new global::TUnit.Core.SourceGenerator.Generators.TestMetadataGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors.Select(e => e.ToString())));
        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        await Assert.That(generated).Contains("Container.Tests<int>");
        await Verify(generated);
    }

    [Test]
    [Arguments("typeof(Hidden)")]
    [Arguments("typeof(Hidden[])")]
    [Arguments("typeof(Hidden[,])")]
    [Arguments("typeof(System.Collections.Generic.List<Hidden>)")]
    [Arguments("typeof(Outer<int>.Inner<string>)")]
    [Arguments("typeof(Outer<>.Inner<>)")]
    public async Task InitializersCompileAndPreservePrivateTypes(string expression)
    {
        var source = $$"""
            public class MarkerAttribute : System.Attribute
            {
                public MarkerAttribute(params System.Type[] types) { Types = types; }
                public System.Type[] Types { get; }
                public System.Type Named { get; set; }
            }
            public class Container
            {
                private sealed class Hidden { }
                private class Outer<T> { public class Inner<U> { } }
                [Marker({{expression}}, Named = {{expression}})]
                public void Run() { }
            }
            """;
        var options = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("PrivateAttributeTypes",
            [CSharpSyntaxTree.ParseText(source, options)], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var attribute = compilation.GetTypeByMetadataName("Container")!.GetMembers("Run").Single().GetAttributes().Single();
        var writer = new AttributeWriter(compilation);
        var initializer = writer.GetAttributeObjectInitializer(attribute);
        var metadataWriter = new global::TUnit.Core.SourceGenerator.CodeWriter("", includeHeader: false);
        writer.WriteAttributeWithoutSyntax(metadataWriter, attribute);
        var generated = $$"""
            public static class Factory
            {
                public static MarkerAttribute Create() => {{initializer}};
                public static MarkerAttribute FromMetadata() => {{metadataWriter}};
            }
            """;
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(generated, options));
        using var stream = new System.IO.MemoryStream();
        var result = compilation.Emit(stream);
        await Assert.That(result.Success).IsTrue().Because(string.Join("\n", result.Diagnostics) + "\n" + generated);
        var assembly = System.Reflection.Assembly.Load(stream.ToArray());
        foreach (var method in new[] { "Create", "FromMetadata" })
        {
            var actual = assembly.GetType("Factory")!.GetMethod(method)!.Invoke(null, null)!;
            var types = (Type[])actual.GetType().GetProperty("Types")!.GetValue(actual)!;
            var named = (Type)actual.GetType().GetProperty("Named")!.GetValue(actual)!;
            var original = assembly.GetType("Container")!.GetMethod("Run")!.GetCustomAttributes(false).Single();
            var expected = (Type[])original.GetType().GetProperty("Types")!.GetValue(original)!;
            await Assert.That(types.Single()).IsEqualTo(expected.Single());
            await Assert.That(named).IsEqualTo(expected.Single());
        }
    }
}
