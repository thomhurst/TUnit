using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Core.SourceGenerator.Generators;
using TUnit.Core.SourceGenerator.Models;

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

    private const string ClassATests =
        """
        using TUnit.Core;

        namespace IncrementalTests;

        public class ClassA
        {
            [Test]
            public void FirstTest()
            {
            }

            [Test]
            [Arguments(1)]
            [Arguments(2)]
            public void SecondTest(int value)
            {
            }
        }
        """;

    private const string OtherTests =
        """
        using TUnit.Core;

        namespace IncrementalTests;

        public class ClassB
        {
            [Test]
            public void ThirdTest()
            {
            }
        }

        [GenerateGenericTest(typeof(int))]
        public class GenericTests<T>
        {
            [Test]
            public void GenericTest()
            {
            }
        }

        public abstract class BaseTests
        {
            [Test]
            public void InheritedTest()
            {
            }
        }

        [InheritsTests]
        public class DerivedTests : BaseTests
        {
        }
        """;

    [Test]
    public async Task UnrelatedEdit_DoesNotRegenerateAnyOutput()
    {
        var compilation1 = CreateStepTrackingCompilation();
        var driver1 = CreateTrackedDriver().RunGenerators(compilation1);
        var runResult1 = GetGeneratorRunResult(driver1);

        // ClassA (2) + ClassB (1) + GenericTests (1), and one [InheritsTests] class.
        await Assert.That(GetStepReasons(runResult1, TestMetadataGenerator.TestMethodResultsStep).Length).IsEqualTo(4);
        await Assert.That(GetStepReasons(runResult1, TestMetadataGenerator.InheritsTestsResultsStep).Length).IsEqualTo(1);
        await Assert.That(GetStepReasons(runResult1, TestMetadataGenerator.ClassTestGroupsStep).Length).IsEqualTo(2);
        await AssertAllReasons(GetSourceOutputReasons(runResult1), IncrementalStepRunReason.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var runResult2 = GetGeneratorRunResult(driver1.RunGenerators(compilation2));

        // Code is regenerated against the new compilation, but it is identical...
        await AssertAllReasons(GetStepReasons(runResult2, TestMetadataGenerator.TestMethodResultsStep), IncrementalStepRunReason.Unchanged);
        await AssertAllReasons(GetStepReasons(runResult2, TestMetadataGenerator.InheritsTestsResultsStep), IncrementalStepRunReason.Unchanged);

        // ...so grouping and every source output are skipped.
        await AssertAllReasons(GetStepReasons(runResult2, TestMetadataGenerator.ClassTestGroupsStep), IncrementalStepRunReason.Cached);
        await AssertAllReasons(GetSourceOutputReasons(runResult2), IncrementalStepRunReason.Cached);
    }

    [Test]
    public async Task EditingTest_RegeneratesOnlyItsClass()
    {
        var compilation1 = CreateStepTrackingCompilation();
        var driver1 = CreateTrackedDriver().RunGenerators(compilation1);

        var classATree = compilation1.SyntaxTrees.Single(t => t.FilePath == "ClassA.cs");
        var compilation2 = ReplaceSyntaxTreeText(compilation1, classATree, "public void FirstTest()", "public void RenamedTest()");
        var runResult2 = GetGeneratorRunResult(driver1.RunGenerators(compilation2));

        var methodReasons = GetStepReasons(runResult2, TestMetadataGenerator.TestMethodResultsStep);
        await Assert.That(methodReasons.Count(reason => reason == IncrementalStepRunReason.Modified)).IsEqualTo(1);
        await Assert.That(methodReasons.Count(reason => reason == IncrementalStepRunReason.Unchanged)).IsEqualTo(3);

        var groups = runResult2.TrackedSteps[TestMetadataGenerator.ClassTestGroupsStep]
            .SelectMany(step => step.Outputs)
            .ToDictionary(output => ((ClassTestGroup)output.Value).ClassFullyQualified, output => output.Reason);
        await Assert.That(groups["global::IncrementalTests.ClassA"]).IsEqualTo(IncrementalStepRunReason.Modified);
        await Assert.That(groups["global::IncrementalTests.ClassB"]).IsEqualTo(IncrementalStepRunReason.Unchanged);

        await AssertAllReasons(GetStepReasons(runResult2, TestMetadataGenerator.InheritsTestsResultsStep), IncrementalStepRunReason.Unchanged);
    }

    [Test]
    public async Task EditingBaseClass_RegeneratesInheritedTests()
    {
        var compilation1 = CreateStepTrackingCompilation();
        var driver1 = CreateTrackedDriver().RunGenerators(compilation1);

        var otherTree = compilation1.SyntaxTrees.Single(t => t.FilePath == "OtherTests.cs");
        var compilation2 = ReplaceSyntaxTreeText(compilation1, otherTree, "public void InheritedTest()", "public void RenamedInheritedTest()");
        var runResult2 = GetGeneratorRunResult(driver1.RunGenerators(compilation2));

        await AssertAllReasons(GetStepReasons(runResult2, TestMetadataGenerator.InheritsTestsResultsStep), IncrementalStepRunReason.Modified);
        await AssertAllReasons(GetStepReasons(runResult2, TestMetadataGenerator.ClassTestGroupsStep), IncrementalStepRunReason.Cached);
    }

    [Test]
    public async Task DisabledSourceGeneration_ProducesNoResults()
    {
        var driver = CreateTrackedDriver().WithUpdatedAnalyzerConfigOptions(new DisabledOptionsProvider());

        var runResult = GetGeneratorRunResult(driver.RunGenerators(CreateStepTrackingCompilation()));

        await Assert.That(runResult.GeneratedSources).IsEmpty();
        await Assert.That(runResult.Diagnostics).IsEmpty();

        // Reflection-mode builds must not pay for code generation.
        if (runResult.TrackedSteps.TryGetValue(TestMetadataGenerator.TestMethodResultsStep, out var steps))
        {
            await Assert.That(steps.SelectMany(step => step.Outputs).Where(output => output.Value is not null)).IsEmpty();
        }
    }

    private static GeneratorDriver CreateTrackedDriver() => CSharpGeneratorDriver.Create(
        [new TestMetadataGenerator().AsSourceGenerator()],
        driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

    private static CSharpCompilation CreateStepTrackingCompilation() => CreateCompilation(
        CSharpSyntaxTree.ParseText(ClassATests, CSharpParseOptions.Default, "ClassA.cs"),
        CSharpSyntaxTree.ParseText(OtherTests, CSharpParseOptions.Default, "OtherTests.cs"));

    private static CSharpCompilation ReplaceSyntaxTreeText(CSharpCompilation compilation, SyntaxTree tree, string oldText, string newText)
    {
        var source = tree.GetText().ToString();
        if (!source.Contains(oldText))
        {
            throw new ArgumentException($"The syntax tree does not contain '{oldText}'.", nameof(oldText));
        }

        return compilation.ReplaceSyntaxTree(tree,
            CSharpSyntaxTree.ParseText(source.Replace(oldText, newText), (CSharpParseOptions)tree.Options, tree.FilePath));
    }

    private static GeneratorRunResult GetGeneratorRunResult(GeneratorDriver driver) =>
        driver.GetRunResult().Results.Single();

    private static IncrementalStepRunReason[] GetStepReasons(GeneratorRunResult runResult, string stepName) =>
        runResult.TrackedSteps[stepName].SelectMany(step => step.Outputs).Select(output => output.Reason).ToArray();

    private static IncrementalStepRunReason[] GetSourceOutputReasons(GeneratorRunResult runResult) =>
        runResult.TrackedOutputSteps["SourceOutput"].SelectMany(step => step.Outputs).Select(output => output.Reason).ToArray();

    private static async Task AssertAllReasons(IncrementalStepRunReason[] reasons, IncrementalStepRunReason expected)
    {
        await Assert.That(reasons).IsNotEmpty();
        await Assert.That(reasons.Where(reason => reason != expected)).IsEmpty();
    }

    private sealed class DisabledOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new DisabledOptions();

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }

    private sealed class DisabledOptions : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            value = key == "build_property.EnableTUnitSourceGeneration" ? "false" : null!;
            return value is not null;
        }
    }

    private static string GetSource(GeneratorDriver driver) =>
        string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString()));

    private static CSharpCompilation CreateCompilation(params SyntaxTree[] trees) => CSharpCompilation.Create(
        "MetadataEdits", trees, ReferencesHelper.References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
