using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

public class PropertyInjectionIncrementalTests
{
    private const string Source = """
        using TUnit.Core;
        public class Fixture { }
        public class Tests
        {
            [ClassDataSource<Fixture>]
            public Fixture Value { get; set; }
        }
        """;

    [Test]
    public async Task AddingAndRemovingReferenceRefreshesCompilationTypes()
    {
        var withoutCore = ReferencesHelper.References
            .Where(reference => !string.Equals(Path.GetFileName(reference.FilePath), "TUnit.Core.dll", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var compilation = CreateCompilation().WithReferences(withoutCore);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new PropertyInjectionSourceGenerator());

        driver = driver.RunGenerators(compilation);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();

        compilation = compilation.WithReferences(ReferencesHelper.References);
        await Assert.That(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        driver = driver.RunGenerators(compilation);
        await Assert.That(GetSource(driver)).Contains("PropertyName = \"Value\"");

        driver = driver.RunGenerators(compilation.WithReferences(withoutCore));
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Assert.That(driver.GetRunResult().Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
    }

    [Test]
    public async Task PropertyEditUpdatesReusedDriver()
    {
        var compilation = CreateCompilation();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new PropertyInjectionSourceGenerator());
        driver = driver.RunGenerators(compilation);
        await Assert.That(GetSource(driver)).Contains("PropertyName = \"Value\"");

        var originalTree = compilation.SyntaxTrees.Single();
        compilation = compilation.ReplaceSyntaxTree(originalTree,
            CSharpSyntaxTree.ParseText(Source.Replace("Value", "Renamed")));
        driver = driver.RunGenerators(compilation);

        await Assert.That(GetSource(driver)).Contains("PropertyName = \"Renamed\"");
        await Assert.That(GetSource(driver)).DoesNotContain("PropertyName = \"Value\"");
        await Assert.That(driver.GetRunResult().Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
    }

    private static string GetSource(GeneratorDriver driver) =>
        string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString()));

    [Test]
    public async Task AddingAndRemovingAttributeUpdatesReusedDriver()
    {
        var plainSource = Source.Replace("[ClassDataSource<Fixture>]", "");
        var compilation = CreateCompilation().RemoveAllSyntaxTrees().AddSyntaxTrees(CSharpSyntaxTree.ParseText(plainSource));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new PropertyInjectionSourceGenerator());
        driver = driver.RunGenerators(compilation);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();

        compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(Source));
        driver = driver.RunGenerators(compilation);
        await Assert.That(GetSource(driver)).Contains("PropertyName = \"Value\"");

        compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(plainSource));
        driver = driver.RunGenerators(compilation);
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsEmpty();
        await Assert.That(driver.GetRunResult().Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
    }

    private static CSharpCompilation CreateCompilation() => CSharpCompilation.Create(
        "PropertyInjectionEdits",
        [CSharpSyntaxTree.ParseText(Source)],
        ReferencesHelper.References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}

