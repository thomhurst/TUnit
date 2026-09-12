using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

public class TestMetadataIncrementalTests
{
    [Test]
    public async Task EditingAnotherFileRefreshesTestMetadata()
    {
        var testTree = CSharpSyntaxTree.ParseText("""
            using TUnit.Core;
            public class Tests
            {
                [Test, Category(Settings.Category)]
                public void Check() { }
            }
            """);
        var settingsTree = CSharpSyntaxTree.ParseText("""
            public static class Settings { public const string Category = "before"; }
            """);
        var compilation = CreateCompilation(testTree, settingsTree);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TestMetadataGenerator());
        driver = driver.RunGenerators(compilation);
        await Assert.That(GetSource(driver)).Contains("before");

        compilation = compilation.ReplaceSyntaxTree(settingsTree,
            CSharpSyntaxTree.ParseText(settingsTree.ToString().Replace("before", "after")));
        driver = driver.RunGenerators(compilation);

        await Assert.That(GetSource(driver)).Contains("after");
        await Assert.That(GetSource(driver)).IsEqualTo(GetSource(
            CSharpGeneratorDriver.Create(new TestMetadataGenerator()).RunGenerators(compilation)));
        await Assert.That(driver.GetRunResult().Diagnostics).IsEmpty();
    }

    [Test]
    public async Task RemovingAndRestoringTestAttributeRefreshesReusedDriver()
    {
        const string source = "using TUnit.Core; public class Tests { [Test] public void Check() { } }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CreateCompilation(tree);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TestMetadataGenerator());
        driver = driver.RunGenerators(compilation);
        var original = GetSource(driver);
        await Assert.That(original).IsNotEmpty();

        var plainTree = CSharpSyntaxTree.ParseText(source.Replace("[Test]", ""));
        var withoutTest = compilation.ReplaceSyntaxTree(tree, plainTree);
        driver = driver.RunGenerators(withoutTest);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();

        driver = driver.RunGenerators(compilation);
        await Assert.That(GetSource(driver)).IsEqualTo(original);
        await Assert.That(driver.GetRunResult().Diagnostics).IsEmpty();
    }

    [Test]
    public async Task AddingAndRemovingCoreReferenceRefreshesReusedDriver()
    {
        var compilation = CreateCompilation(CSharpSyntaxTree.ParseText(
            "using TUnit.Core; public class Tests { [Test] public void Check() { } }"));
        var withoutCore = compilation.WithReferences(ReferencesHelper.References.Where(reference =>
            !string.Equals(Path.GetFileName(reference.FilePath), "TUnit.Core.dll", StringComparison.OrdinalIgnoreCase)));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TestMetadataGenerator());
        driver = driver.RunGenerators(withoutCore);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();

        driver = driver.RunGenerators(compilation);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsNotEmpty();

        driver = driver.RunGenerators(withoutCore);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Assert.That(driver.GetRunResult().Diagnostics).IsEmpty();
    }

    private static string GetSource(GeneratorDriver driver) =>
        string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString()));

    private static CSharpCompilation CreateCompilation(params SyntaxTree[] trees) => CSharpCompilation.Create(
        "MetadataEdits", trees, ReferencesHelper.References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
