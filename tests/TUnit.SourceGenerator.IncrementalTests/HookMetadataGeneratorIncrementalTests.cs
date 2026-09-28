using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

public class HookMetadataGeneratorIncrementalTests
{
    private const string DefaultSource =
        """
        using System.Threading.Tasks;
        using TUnit.Core;

        public class HookTests
        {
            [Before(HookType.Test)]
            public void Setup()
            {
            }
        }
        """;

    [Fact]
    public void AddUnrelatedType_ShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<HookMetadataGenerator>(compilation1);
        AssertRunReason(driver1, IncrementalStepRunReason.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReason(driver2, IncrementalStepRunReason.Cached);
    }

    [Fact]
    public void MoveHookToDifferentLine_ShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<HookMetadataGenerator>(compilation1);
        AssertRunReason(driver1, IncrementalStepRunReason.New);

        // Same hook shifted down two lines: the emitted LineNumber must follow it.
        var compilation2 = compilation1.ReplaceSyntaxTree(
            compilation1.SyntaxTrees.Single(),
            CSharpSyntaxTree.ParseText(DefaultSource.Replace("public class HookTests", "\n\npublic class HookTests"), CSharpParseOptions.Default));

        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReason(driver2, IncrementalStepRunReason.Modified);
    }

    [Fact]
    public void MakeHookAsync_ShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<HookMetadataGenerator>(compilation1);
        AssertRunReason(driver1, IncrementalStepRunReason.New);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "Setup",
            """
            [Before(HookType.Test)]
            public async Task Setup()
            {
                await Task.Yield();
            }
            """);

        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReason(driver2, IncrementalStepRunReason.Modified);
    }

    private static void AssertRunReason(GeneratorDriver driver, IncrementalStepRunReason reason)
    {
        var runResult = driver.GetRunResult().Results[0];

        TestHelper.AssertRunReason(runResult, HookMetadataGenerator.ExtractBeforeHooks, reason, 0);
    }
}
