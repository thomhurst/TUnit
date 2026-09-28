using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

public class InfrastructureGeneratorIncrementalTests
{
    private const string DefaultSource =
        """
        using TUnit.Core;

        public class Tests
        {
            [Test]
            public void Test1()
            {
            }
        }
        """;

    [Fact]
    public void EditSource_ShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<InfrastructureGenerator>(compilation1);
        AssertRunReason(driver1, IncrementalStepRunReason.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReason(driver2, IncrementalStepRunReason.Cached);

        var compilation3 = TestHelper.ReplaceMethodDeclaration(compilation1, "Test1",
            """
            [Test]
            public void Test1()
            {
                var x = 1;
            }
            """);
        var driver3 = driver2.RunGenerators(compilation3);
        AssertRunReason(driver3, IncrementalStepRunReason.Cached);
    }

    [Fact]
    public void AddReference_ShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<InfrastructureGenerator>(compilation1);
        AssertRunReason(driver1, IncrementalStepRunReason.New);

        // A library that references TUnit.Core must be pre-loaded by TUnitInfrastructure.g.cs.
        var library = CSharpCompilation.Create(
            "OtherTestLibrary",
            [CSharpSyntaxTree.ParseText("namespace OtherTestLibrary { public class SharedHooks { [TUnit.Core.Before(TUnit.Core.HookType.Assembly)] public static void Setup() { } } }")],
            compilation1.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilation2 = compilation1.AddReferences(library.ToMetadataReference());
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReason(driver2, IncrementalStepRunReason.Modified);

        var generated = driver2.GetRunResult().GeneratedTrees.Single(t => t.FilePath.EndsWith("TUnitInfrastructure.g.cs"));
        Xunit.Assert.Contains("global::OtherTestLibrary.SharedHooks", generated.ToString());
    }

    private static void AssertRunReason(GeneratorDriver driver, IncrementalStepRunReason reason)
    {
        var runResult = driver.GetRunResult().Results[0];

        TestHelper.AssertRunReason(runResult, InfrastructureGenerator.ExtractAssemblyInfoStep, reason, 0);
    }
}
