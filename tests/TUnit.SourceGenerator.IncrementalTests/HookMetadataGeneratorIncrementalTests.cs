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
        TestHelper.AssertRunReason(driver2.GetRunResult().Results[0], HookMetadataGenerator.HookClassGroups, IncrementalStepRunReason.Cached, 0);
    }

    [Fact]
    public void TypesWithSameSanitizedName_ShouldGenerateDistinctFilesAndMembers()
    {
        // A_B.C and A.B_C both sanitize to A_B_C. Hint names and the members emitted into the shared
        // TUnit_HookRegistration partial class must still be unique.
        const string source =
            """
            using TUnit.Core;

            namespace A_B
            {
                public class C
                {
                    [Before(HookType.Test)]
                    public void Setup()
                    {
                    }
                }
            }

            namespace A
            {
                public class B_C
                {
                    [Before(HookType.Test)]
                    public void Setup()
                    {
                    }
                }
            }
            """;

        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default));

        var driver = TestHelper.GenerateTracked<HookMetadataGenerator>(compilation);
        var runResult = driver.GetRunResult();

        Xunit.Assert.Empty(runResult.Diagnostics);
        var generatorResult = Xunit.Assert.Single(runResult.Results);
        Xunit.Assert.Null(generatorResult.Exception);
        Xunit.Assert.Equal(2, generatorResult.GeneratedSources.Length);
        Xunit.Assert.Equal(2, generatorResult.GeneratedSources.Select(static s => s.HintName).Distinct().Count());

        var outputCompilation = compilation.AddSyntaxTrees(generatorResult.GeneratedSources.Select(static s => s.SyntaxTree));
        Xunit.Assert.Empty(outputCompilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void ChangeHookInOneClass_ShouldOnlyModifyThatClassGroup()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using TUnit.Core;

            public class FirstHookTests
            {
                [Before(HookType.Test)]
                public void Setup()
                {
                }
            }

            public class SecondHookTests
            {
                [Before(HookType.Test)]
                public void OtherSetup()
                {
                }
            }
            """;

        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<HookMetadataGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "OtherSetup",
            """
            [Before(HookType.Test)]
            public async Task OtherSetup()
            {
                await Task.Yield();
            }
            """);

        var driver2 = driver1.RunGenerators(compilation2);
        var runResult = driver2.GetRunResult().Results[0];

        TestHelper.AssertRunReason(runResult, HookMetadataGenerator.HookClassGroups, IncrementalStepRunReason.Unchanged, 0);
        TestHelper.AssertRunReason(runResult, HookMetadataGenerator.HookClassGroups, IncrementalStepRunReason.Modified, 1);
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
