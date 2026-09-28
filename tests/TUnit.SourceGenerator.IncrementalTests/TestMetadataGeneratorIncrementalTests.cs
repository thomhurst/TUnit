using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

using Assert = Xunit.Assert;

public class TestMetadataGeneratorIncrementalTests
{
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

    [Fact]
    public void UnrelatedEdit_DoesNotRegenerateAnyOutput()
    {
        var compilation1 = CreateCompilation();

        var driver1 = TestHelper.GenerateTracked<TestMetadataGenerator>(compilation1);
        var runResult1 = GetRunResult(driver1);

        // ClassA (2) + ClassB (1) + GenericTests (1), and one [InheritsTests] class.
        Assert.Equal(4, TestHelper.GetStepReasons(runResult1, TestMetadataGenerator.TestMethodResultsStep).Length);
        Assert.Single(TestHelper.GetStepReasons(runResult1, TestMetadataGenerator.InheritsTestsResultsStep));
        Assert.Equal(2, TestHelper.GetStepReasons(runResult1, TestMetadataGenerator.ClassTestGroupsStep).Length);
        Assert.All(TestHelper.GetSourceOutputReasons(runResult1), reason => Assert.Equal(IncrementalStepRunReason.New, reason));

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var runResult2 = GetRunResult(driver1.RunGenerators(compilation2));

        // Code is regenerated against the new compilation, but it is identical...
        Assert.All(TestHelper.GetStepReasons(runResult2, TestMetadataGenerator.TestMethodResultsStep),
            reason => Assert.Equal(IncrementalStepRunReason.Unchanged, reason));
        Assert.All(TestHelper.GetStepReasons(runResult2, TestMetadataGenerator.InheritsTestsResultsStep),
            reason => Assert.Equal(IncrementalStepRunReason.Unchanged, reason));

        // ...so grouping and every source output are skipped.
        Assert.All(TestHelper.GetStepReasons(runResult2, TestMetadataGenerator.ClassTestGroupsStep),
            reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
        Assert.All(TestHelper.GetSourceOutputReasons(runResult2),
            reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
    }

    [Fact]
    public void EditingTest_RegeneratesOnlyItsClass()
    {
        var compilation1 = CreateCompilation();
        var driver1 = TestHelper.GenerateTracked<TestMetadataGenerator>(compilation1);

        var classATree = compilation1.SyntaxTrees.Single(t => t.FilePath == "ClassA.cs");
        var compilation2 = TestHelper.ReplaceSyntaxTreeText(compilation1, classATree, "public void FirstTest()", "public void RenamedTest()");
        var runResult2 = GetRunResult(driver1.RunGenerators(compilation2));

        var methodReasons = TestHelper.GetStepReasons(runResult2, TestMetadataGenerator.TestMethodResultsStep);
        Assert.Equal(1, methodReasons.Count(reason => reason == IncrementalStepRunReason.Modified));
        Assert.Equal(3, methodReasons.Count(reason => reason == IncrementalStepRunReason.Unchanged));

        var groups = TestHelper.GetStepOutputs(runResult2, TestMetadataGenerator.ClassTestGroupsStep)
            .ToDictionary(output => ((ClassTestGroup)output.Value).ClassFullyQualified, output => output.Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, groups["global::IncrementalTests.ClassA"]);
        Assert.Equal(IncrementalStepRunReason.Unchanged, groups["global::IncrementalTests.ClassB"]);

        Assert.All(TestHelper.GetStepReasons(runResult2, TestMetadataGenerator.InheritsTestsResultsStep),
            reason => Assert.Equal(IncrementalStepRunReason.Unchanged, reason));
    }

    [Fact]
    public void EditingBaseClass_RegeneratesInheritedTests()
    {
        var compilation1 = CreateCompilation();
        var driver1 = TestHelper.GenerateTracked<TestMetadataGenerator>(compilation1);

        var otherTree = compilation1.SyntaxTrees.Single(t => t.FilePath == "OtherTests.cs");
        var compilation2 = TestHelper.ReplaceSyntaxTreeText(compilation1, otherTree, "public void InheritedTest()", "public void RenamedInheritedTest()");
        var runResult2 = GetRunResult(driver1.RunGenerators(compilation2));

        Assert.All(TestHelper.GetStepReasons(runResult2, TestMetadataGenerator.InheritsTestsResultsStep),
            reason => Assert.Equal(IncrementalStepRunReason.Modified, reason));
        Assert.All(TestHelper.GetStepReasons(runResult2, TestMetadataGenerator.ClassTestGroupsStep),
            reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
    }

    [Fact]
    public void DisabledSourceGeneration_ProducesNoResults()
    {
        var compilation = CreateCompilation();

        var driver = CSharpGeneratorDriver.Create(
                [new TestMetadataGenerator().AsSourceGenerator()],
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true))
            .WithUpdatedAnalyzerConfigOptions(new DisabledOptionsProvider());

        var runResult = GetRunResult(driver.RunGenerators(compilation));

        Assert.Empty(runResult.GeneratedSources);
        Assert.Empty(runResult.Diagnostics);

        // Reflection-mode builds must not pay for code generation.
        if (runResult.TrackedSteps.TryGetValue(TestMetadataGenerator.TestMethodResultsStep, out var steps))
        {
            Assert.All(steps.SelectMany(step => step.Outputs), output => Assert.Null(output.Value));
        }
    }

    private static CSharpCompilation CreateCompilation() =>
        Fixture.CreateLibrary(
            CSharpSyntaxTree.ParseText(ClassATests, CSharpParseOptions.Default, "ClassA.cs"),
            CSharpSyntaxTree.ParseText(OtherTests, CSharpParseOptions.Default, "OtherTests.cs"));

    private static GeneratorRunResult GetRunResult(GeneratorDriver driver) =>
        driver.GetRunResult().Results.Single();

    private sealed class DisabledOptionsProvider : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptionsProvider
    {
        public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GlobalOptions { get; } = new DisabledOptions();

        public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }

    private sealed class DisabledOptions : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            value = key == "build_property.EnableTUnitSourceGeneration" ? "false" : null!;
            return value is not null;
        }
    }
}
