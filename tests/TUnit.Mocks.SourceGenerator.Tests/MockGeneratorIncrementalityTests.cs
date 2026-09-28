using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Checks which pipeline steps re-run between generator passes. Emitting a mock is by far the most
/// expensive step, so edits that do not change a mocked type must leave its emit step cached.
/// </summary>
public class MockGeneratorIncrementalityTests : SnapshotTestBase
{
    private static readonly CSharpParseOptions ParseOptions =
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    private const string Contracts = """
        namespace TestNamespace;

        public interface IGreeter
        {
            string Greet(string name);
            IClock Clock { get; }
        }

        public interface IClock
        {
            int Now();
        }

        public abstract class Repository
        {
            public abstract int Count();
        }
        """;

    private const string Usage = """
        using TUnit.Mocks;

        namespace TestNamespace;

        public class Usage
        {
            void M()
            {
                _ = Mock.Of<IGreeter>();
                _ = IGreeter.Mock();
                _ = Mock.Of<Repository>();
            }
        }
        """;

    [Test]
    public async Task Unrelated_Edit_Leaves_Emitted_Source_Cached()
    {
        var compilation = CreateCompilation(Contracts, Usage);
        var driver = RunTracked(CreateDriver(), compilation);

        var edited = compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText("namespace TestNamespace; public class Unrelated { }", ParseOptions));
        driver = RunTracked(driver, edited);

        var result = driver.GetRunResult().Results.Single();
        await AssertAllOutputs(result, MockTrackingNames.DistinctModels, IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        await AssertAllOutputs(result, MockTrackingNames.EmitResults, IncrementalStepRunReason.Cached);
    }

    [Test]
    public async Task Moving_A_Call_Site_Leaves_Emitted_Source_Cached()
    {
        var compilation = CreateCompilation(Contracts, Usage);
        var driver = RunTracked(CreateDriver(), compilation);
        var firstSources = GetGeneratedSources(driver);

        // A blank line above the call sites shifts every request location.
        var usageTree = compilation.SyntaxTrees.Single(t => t.ToString().Contains("class Usage"));
        var edited = compilation.ReplaceSyntaxTree(
            usageTree,
            CSharpSyntaxTree.ParseText(Usage.Replace("public class Usage", "\npublic class Usage"), ParseOptions));
        driver = RunTracked(driver, edited);

        var result = driver.GetRunResult().Results.Single();

        // The requests did change (their locations moved), so the test is not vacuous...
        await Assert.That(result.TrackedSteps[MockTrackingNames.DistinctRequests]
                .SelectMany(step => step.Outputs)
                .Any(output => output.Reason == IncrementalStepRunReason.Modified))
            .IsTrue();

        // ...but the models, and therefore the generated source, did not.
        await AssertAllOutputs(result, MockTrackingNames.DistinctModels, IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        await AssertAllOutputs(result, MockTrackingNames.EmitResults, IncrementalStepRunReason.Cached);
        await Assert.That(GetGeneratedSources(driver)).IsEquivalentTo(firstSources);
    }

    [Test]
    public async Task Changing_A_Mocked_Interface_Regenerates_Its_Source()
    {
        var compilation = CreateCompilation(Contracts, Usage);
        var driver = RunTracked(CreateDriver(), compilation);

        var contractsTree = compilation.SyntaxTrees.Single(t => t.ToString().Contains("interface IGreeter"));
        var edited = compilation.ReplaceSyntaxTree(
            contractsTree,
            CSharpSyntaxTree.ParseText(
                Contracts.Replace("string Greet(string name);", "string Greet(string name);\n    void Wave();"),
                ParseOptions));
        driver = RunTracked(driver, edited);

        var result = driver.GetRunResult().Results.Single();
        var emitOutputs = result.TrackedSteps[MockTrackingNames.EmitResults]
            .SelectMany(step => step.Outputs)
            .ToList();

        await Assert.That(emitOutputs.Any(output => output.Reason == IncrementalStepRunReason.Modified)).IsTrue();
        await Assert.That(GetGeneratedSources(driver).Any(source => source.Contains("Wave", StringComparison.Ordinal))).IsTrue();
    }

    private static async Task AssertAllOutputs(
        GeneratorRunResult result,
        string stepName,
        params IncrementalStepRunReason[] allowedReasons)
    {
        var reasons = result.TrackedSteps[stepName]
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToList();

        await Assert.That(reasons).IsNotEmpty();
        foreach (var reason in reasons)
        {
            await Assert.That(allowedReasons).Contains(reason);
        }
    }

    private static CSharpCompilation CreateCompilation(params string[] sources)
        => CSharpCompilation.Create(
            "TestAssembly",
            sources.Select(source => CSharpSyntaxTree.ParseText(source, ParseOptions)),
            GetCachedReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static GeneratorDriver CreateDriver()
        => CSharpGeneratorDriver.Create(
            [new MockGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

    private static GeneratorDriver RunTracked(GeneratorDriver driver, Compilation compilation)
    {
        driver = driver.RunGenerators(compilation);

        var errors = driver.GetRunResult().Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        return driver;
    }

    private static string[] GetGeneratedSources(GeneratorDriver driver)
        => driver.GetRunResult().GeneratedTrees
            .Select(tree => tree.GetText().ToString())
            .ToArray();
}
