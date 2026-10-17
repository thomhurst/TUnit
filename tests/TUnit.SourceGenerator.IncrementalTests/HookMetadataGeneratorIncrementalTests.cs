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

    [Test]
    public async Task AddUnrelatedType_ShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<HookMetadataGenerator>(compilation1);
        await AssertRunReason(driver1, IncrementalStepRunReason.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReason(driver2, IncrementalStepRunReason.Cached);
        await TestHelper.AssertRunReason(driver2.GetRunResult().Results[0], HookMetadataGenerator.HookClassGroups, IncrementalStepRunReason.Cached, 0);
    }

    [Test]
    public async Task TypesWithSameSanitizedName_ShouldGenerateDistinctFilesAndMembers()
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

        await Assert.That(runResult.Diagnostics).IsEmpty();
        var generatorResult = await Assert.That(runResult.Results).HasSingleItem();
        await Assert.That(generatorResult.Exception).IsNull();
        await Assert.That(generatorResult.GeneratedSources.Length).IsEqualTo(2);
        await Assert.That(generatorResult.GeneratedSources.Select(static s => s.HintName).Distinct().Count()).IsEqualTo(2);

        var outputCompilation = compilation.AddSyntaxTrees(generatorResult.GeneratedSources.Select(static s => s.SyntaxTree));
        await Assert.That(outputCompilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
    }

    [Test]
    public async Task ChangeHookInOneClass_ShouldOnlyModifyThatClassGroup()
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

        await TestHelper.AssertRunReason(runResult, HookMetadataGenerator.HookClassGroups, IncrementalStepRunReason.Unchanged, 0);
        await TestHelper.AssertRunReason(runResult, HookMetadataGenerator.HookClassGroups, IncrementalStepRunReason.Modified, 1);
    }

    [Test]
    public async Task MoveHookToDifferentLine_ShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<HookMetadataGenerator>(compilation1);
        await AssertRunReason(driver1, IncrementalStepRunReason.New);

        // Same hook shifted down two lines: the emitted LineNumber must follow it.
        var compilation2 = compilation1.ReplaceSyntaxTree(
            compilation1.SyntaxTrees.Single(),
            CSharpSyntaxTree.ParseText(DefaultSource.Replace("public class HookTests", "\n\npublic class HookTests"), CSharpParseOptions.Default));

        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReason(driver2, IncrementalStepRunReason.Modified);
    }

    [Test]
    public async Task MakeHookAsync_ShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<HookMetadataGenerator>(compilation1);
        await AssertRunReason(driver1, IncrementalStepRunReason.New);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "Setup",
            """
            [Before(HookType.Test)]
            public async Task Setup()
            {
                await Task.Yield();
            }
            """);

        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReason(driver2, IncrementalStepRunReason.Modified);
    }

    [Test]
    public async Task ClassGroups_AcrossFilesWithDisjointHookKinds_AreEmittedInCompilationOrder()
    {
        // The first file's class has only a Before hook besides the shared BeforeEvery hook, and the second
        // file's class has only an After hook. No single hook kind orders the two files except BeforeEvery,
        // so file order must come from the compilation rather than from merging per-kind sequences.
        const string first =
            """
            using TUnit.Core;

            public static class FirstFileHooks
            {
                [Before(HookType.Class)]
                public static void BeforeClass() { }

                [BeforeEvery(HookType.Assembly)]
                public static void BeforeEveryAssembly(AssemblyHookContext context) { }
            }
            """;

        const string second =
            """
            using TUnit.Core;

            public static class SecondFileHooks
            {
                [After(HookType.Class)]
                public static void AfterClass() { }

                [BeforeEvery(HookType.Assembly)]
                public static void BeforeEveryAssembly(AssemblyHookContext context) { }
            }
            """;

        var compilation = Fixture.CreateLibrary(
            CSharpSyntaxTree.ParseText(first, CSharpParseOptions.Default, path: "First.cs"),
            CSharpSyntaxTree.ParseText(second, CSharpParseOptions.Default, path: "Second.cs"));

        await AssertGeneratedOrder(compilation, "FirstFileHooks", "SecondFileHooks");

        // Reversing the files in the compilation reverses the emitted order.
        var reversed = Fixture.CreateLibrary(
            CSharpSyntaxTree.ParseText(second, CSharpParseOptions.Default, path: "Second.cs"),
            CSharpSyntaxTree.ParseText(first, CSharpParseOptions.Default, path: "First.cs"));

        await AssertGeneratedOrder(reversed, "SecondFileHooks", "FirstFileHooks");
    }

    [Test]
    public async Task ClassGroups_DeclaredOnOneLine_AreEmittedInDeclarationOrder()
    {
        const string source =
            """
            using TUnit.Core;

            public class ZHooks { [Before(HookType.Test)] public void Setup() { } } public class AHooks { [Before(HookType.Test)] public void Setup() { } }
            """;

        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default));

        await AssertGeneratedOrder(compilation, "ZHooks", "AHooks");
    }

    [Test]
    public async Task AddUnrelatedFile_ShouldNotChangeHookFileOrder()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default, path: "Hooks.cs"));

        var driver1 = TestHelper.GenerateTracked<HookMetadataGenerator>(compilation1);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}", CSharpParseOptions.Default, path: "Other.cs"));
        var driver2 = driver1.RunGenerators(compilation2);
        var runResult = driver2.GetRunResult().Results[0];

        await TestHelper.AssertRunReason(runResult, HookMetadataGenerator.HookFileOrder, IncrementalStepRunReason.Unchanged, 0);
        await TestHelper.AssertRunReason(runResult, HookMetadataGenerator.HookClassGroups, IncrementalStepRunReason.Cached, 0);
    }

    private static async Task AssertGeneratedOrder(Compilation compilation, params string[] typeNames)
    {
        var driver = TestHelper.GenerateTracked<HookMetadataGenerator>(compilation);
        var generatorResult = await Assert.That(driver.GetRunResult().Results).HasSingleItem();
        await Assert.That(generatorResult.Exception).IsNull();

        var hintNames = generatorResult.GeneratedSources.Select(static s => s.HintName).ToArray();
        var indices = typeNames
            .Select(typeName => Array.FindIndex(hintNames, hintName => hintName.Contains(typeName)))
            .ToArray();

        await Assert.That(indices).DoesNotContain(-1);
        await Assert.That(indices).IsInOrder();
    }

    private static async Task AssertRunReason(GeneratorDriver driver, IncrementalStepRunReason reason)
    {
        var runResult = driver.GetRunResult().Results[0];

        await TestHelper.AssertRunReason(runResult, HookMetadataGenerator.ExtractBeforeHooks, reason, 0);
    }
}