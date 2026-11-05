using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

public class KeywordTypeExpressionTests
{
    [Test]
    public async Task MetadataAttributeTypeValuesCompile()
    {
        var source = CreateCompilation("""
            namespace @event
            {
                public class @class { }
                public class @struct<T> { }
            }
            public class MarkerAttribute : System.Attribute
            {
                public System.Type Value { get; set; }
                public System.Type[] Values { get; set; }
            }
            [Marker(Value = typeof(@event.@class), Values = new[] { typeof(@event.@struct<>), typeof(@event.@class[]) })]
            public class Subject { }
            """);
        using var image = new MemoryStream();
        await Assert.That(source.Emit(image).Success).IsTrue();
        var consumer = CreateCompilation("").AddReferences(MetadataReference.CreateFromImage(image.ToArray()));
        var attribute = consumer.GetTypeByMetadataName("Subject")!.GetAttributes().Single();
        await Assert.That(attribute.ApplicationSyntaxReference).IsNull();
        var initializer = new AttributeWriter(consumer).GetAttributeObjectInitializer(attribute);
        await Assert.That(initializer).Contains("typeof(global::@event.@class)");
        await Assert.That(initializer).Contains("typeof(global::@event.@struct<>)");
        await Assert.That(initializer).Contains("typeof(global::@event.@class[])");
        var output = consumer.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            $"public class Consumer {{ public object Create() => {initializer}; }}", new CSharpParseOptions(LanguageVersion.Preview)));
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
    }

    [Test]
    public async Task TypeExpressionsCompileAndRuntimeNamesStayUnescaped()
    {
        var compilation = CreateCompilation("""
            namespace @event
            {
                public class @class { }
                public class @struct<T> { }
                public class Subject
                {
                    public @struct<@class>[] Value;
                }
            }
            """);
        var type = compilation.GetTypeByMetadataName("event.class")!;
        var generic = compilation.GetTypeByMetadataName("event.struct`1")!;
        var array = ((IFieldSymbol)compilation.GetTypeByMetadataName("event.Subject")!.GetMembers("Value").Single()).Type;
        var expressions = new[] { type.GloballyQualified(), generic.GloballyQualified(), array.GloballyQualified() };
        var output = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            $"public class Consumer {{ public System.Type[] Types => new[] {{ {string.Join(", ", expressions.Select(e => $"typeof({e})"))} }}; }}", new CSharpParseOptions(LanguageVersion.Preview)));
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix)).IsEqualTo("event.class");
        await Assert.That(type.GloballyQualifiedNonGeneric()).IsEqualTo("global::event.class");
    }

    [Test]
    public async Task TestMetadataTypeExpressionsCompile()
    {
        var compilation = CreateCompilation("""
            namespace @event
            {
                public class @class
                {
                    [TUnit.Core.Before(TUnit.Core.HookType.Test)]
                    public void Setup() { }

                    [TUnit.Core.Test]
                    [TUnit.Core.Arguments(null)]
                    public void Run(@struct<@class>[] value) { }
                }
                public class @struct<T> { }
            }
            """);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMetadataGenerator().AsSourceGenerator(), new HookMetadataGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        await Assert.That(driver.GetRunResult().Results.Select(r => r.Exception).FirstOrDefault(e => e is not null)).IsNull();
        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
    }

    [Test]
    public async Task EscapedAndUnderscoreNamespacesProduceDistinctTestSources()
    {
        var compilation = CreateCompilation("""
            namespace @event
            {
                public class Subject { [TUnit.Core.Test] public void Run() { } }
            }
            namespace _event
            {
                public class Subject { [TUnit.Core.Test] public void Run() { } }
            }
            """);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMetadataGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        await Assert.That(driver.GetRunResult().Results.Select(r => r.Exception).FirstOrDefault(e => e is not null)).IsNull();
        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(driver.GetRunResult().Results.Single().GeneratedSources.Count(s => s.HintName.EndsWith("__TestSource.g.cs"))).IsEqualTo(2);
    }

    [Test]
    public async Task PropertyInjectionInKeywordNamespaceCompiles()
    {
        var compilation = CreateCompilation("""
            namespace @event
            {
                public class @class
                {
                    [TUnit.Core.Arguments("value")]
                    public string Value { get; set; }
                }
            }
            """);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new PropertyInjectionSourceGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        await Assert.That(driver.GetRunResult().Results.Select(r => r.Exception).FirstOrDefault(e => e is not null)).IsNull();
        await Assert.That(driver.GetRunResult().Results.Single().GeneratedSources).IsNotEmpty();
        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
    }

    private static CSharpCompilation CreateCompilation(string source) => CSharpCompilation.Create(
        "KeywordExpressions",
        [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
        ReferencesHelper.References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
