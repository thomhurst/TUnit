using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;

namespace TUnit.Core.SourceGenerator.Tests;

public class AttributeInitializerCacheTests
{
    [Test]
    public async Task ArgumentFreeApplicationsDoNotReplaceExplicitArguments()
    {
        var compilation = CreateCompilation("""
            public class MarkerAttribute : System.Attribute
            {
                public MarkerAttribute(int value = 0) { }
                public int Value { get; set; }
            }
            public class Tests
            {
                [Marker] public void First() { }
                [Marker()] public void Second() { }
                [Marker(42)] public void Explicit() { }
                [Marker(Value = 17)] public void Named() { }
            }
            """);
        var writer = new AttributeWriter(compilation);

        await Assert.That(Initializer("First")).IsEqualTo("new global::MarkerAttribute()");
        await Assert.That(Initializer("Second")).IsEqualTo("new global::MarkerAttribute()");
        await Assert.That(Initializer("Explicit")).IsEqualTo("new global::MarkerAttribute(42)");
        await Assert.That(Initializer("Named")).Contains("Value = 17");
        await Assert.That(Initializer("First")).IsEqualTo("new global::MarkerAttribute()");

        string Initializer(string method) => writer.GetAttributeObjectInitializer(
            compilation.GetTypeByMetadataName("Tests")!.GetMembers(method).Single().GetAttributes().Single());
    }

    [Test]
    public async Task ConstructedAttributeTypesKeepDistinctInitializers()
    {
        var compilation = CreateCompilation("""
            public class MarkerAttribute<T> : System.Attribute { }
            public class Tests
            {
                [Marker<int>] public void First() { }
                [Marker<string>] public void Second() { }
            }
            """);
        var writer = new AttributeWriter(compilation);
        var methods = compilation.GetTypeByMetadataName("Tests")!.GetMembers();
        var first = writer.GetAttributeObjectInitializer(methods.Single(m => m.Name == "First").GetAttributes().Single());
        var second = writer.GetAttributeObjectInitializer(methods.Single(m => m.Name == "Second").GetAttributes().Single());

        await Assert.That(first).IsNotEqualTo(second);
        await Assert.That(first).Contains("MarkerAttribute<int>");
        await Assert.That(second).Contains("MarkerAttribute<string>");
    }

    private static CSharpCompilation CreateCompilation(string source) => CSharpCompilation.Create(
        "AttributeInitializers",
        [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
        ReferencesHelper.References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
