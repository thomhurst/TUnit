using System.Runtime.CompilerServices;
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

    [Test]
    public async Task EditSource_ShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<InfrastructureGenerator>(compilation1);
        await AssertRunReason(driver1, IncrementalStepRunReason.New);

        // Syntax-only edits rerun the cheap Select, whose model is unchanged, so the generated source
        // stays cached. Memo_SyntaxOnlyEdits_ReuseModel checks that the reference walk is skipped.
        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReason(driver2, IncrementalStepRunReason.Unchanged);
        await TestHelper.AssertSourceOutputsCached(driver2.GetRunResult().Results[0]);

        var compilation3 = TestHelper.ReplaceMethodDeclaration(compilation1, "Test1",
            """
            [Test]
            public void Test1()
            {
                var x = 1;
            }
            """);
        var driver3 = driver2.RunGenerators(compilation3);
        await AssertRunReason(driver3, IncrementalStepRunReason.Unchanged);
        await TestHelper.AssertSourceOutputsCached(driver3.GetRunResult().Results[0]);
    }

    [Test]
    public async Task EditSource_DoesNotRetainPreviousCompilation()
    {
        var (driver, firstCompilation, latestCompilation) = RunSourceEdits(10);

        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // A comparer that reports the new compilation as equal makes the input node keep the old
        // one, pinning its syntax trees and bound state for as long as the driver lives.
        await Assert.That(firstCompilation.IsAlive).IsFalse().Because("The generator driver kept the first compilation alive after syntax-only edits.");
        GC.KeepAlive(driver);
        GC.KeepAlive(latestCompilation);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (GeneratorDriver Driver, WeakReference FirstCompilation, Compilation LatestCompilation) RunSourceEdits(int edits)
    {
        Compilation compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));
        var firstCompilation = new WeakReference(compilation);

        var driver = CSharpGeneratorDriver
            .Create([new InfrastructureGenerator().AsSourceGenerator()])
            .RunGenerators(compilation);

        for (var i = 0; i < edits; i++)
        {
            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(DefaultSource + $"\npublic class Edit{i} {{ }}", CSharpParseOptions.Default));
            driver = driver.RunGenerators(compilation);
        }

        return (driver, firstCompilation, compilation);
    }

    private static object? GetExtractedModel(GeneratorDriver driver) =>
        driver.GetRunResult().Results[0].TrackedSteps[InfrastructureGenerator.ExtractAssemblyInfoStep][0].Outputs[0].Value;

    [Test]
    public async Task AddReference_ShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<InfrastructureGenerator>(compilation1);
        await AssertRunReason(driver1, IncrementalStepRunReason.New);

        // A library that references TUnit.Core must be pre-loaded by TUnitInfrastructure.g.cs.
        var library = CSharpCompilation.Create(
            "OtherTestLibrary",
            [CSharpSyntaxTree.ParseText("namespace OtherTestLibrary { public class SharedHooks { [TUnit.Core.Before(TUnit.Core.HookType.Assembly)] public static void Setup() { } } }")],
            compilation1.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilation2 = compilation1.AddReferences(library.ToMetadataReference());
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReason(driver2, IncrementalStepRunReason.Modified);

        var generated = driver2.GetRunResult().GeneratedTrees.Single(t => t.FilePath.EndsWith("TUnitInfrastructure.g.cs"));
        await Assert.That(generated.ToString()).Contains("global::OtherTestLibrary.SharedHooks");
    }

    [Test]
    public async Task EditProjectReference_ShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var library1 = CreateHookLibrary(compilation1, "SharedHooks");
        compilation1 = compilation1.AddReferences(library1.ToMetadataReference());

        var driver1 = TestHelper.GenerateTracked<InfrastructureGenerator>(compilation1);
        await AssertRunReason(driver1, IncrementalStepRunReason.New);

        // In the IDE, editing a referenced project replaces its CompilationReference while the
        // assembly name stays the same. Renaming the only public type must reach the output,
        // otherwise TUnitInfrastructure.g.cs keeps a typeof() to a type that no longer exists.
        var library2 = CreateHookLibrary(compilation1, "RenamedHooks");
        var compilation2 = compilation1.ReplaceReference(compilation1.References.Last(), library2.ToMetadataReference());
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReason(driver2, IncrementalStepRunReason.Modified);

        var generated = driver2.GetRunResult().GeneratedTrees.Single(t => t.FilePath.EndsWith("TUnitInfrastructure.g.cs")).ToString();
        await Assert.That(generated).Contains("global::OtherTestLibrary.RenamedHooks");
        await Assert.That(generated).DoesNotContain("global::OtherTestLibrary.SharedHooks");

        // An edit that does not change the extracted model reruns extraction but keeps the output cached.
        var library3 = CreateHookLibrary(compilation1, "RenamedHooks");
        var compilation3 = compilation2.ReplaceReference(compilation2.References.Last(), library3.ToMetadataReference());
        var driver3 = driver2.RunGenerators(compilation3);
        await AssertRunReason(driver3, IncrementalStepRunReason.Unchanged);
    }

    private const string ShadowingSource = "namespace OtherTestLibrary { public class FirstHooks { } }";

    [Test]
    public async Task FreshDriver_SameReferences_DoesNotShareMemo()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        // The memo belongs to one driver, so an unrelated driver over the same reference list
        // extracts fresh instead of reusing another driver's result.
        var first = GetExtractedModel(TestHelper.GenerateTracked<InfrastructureGenerator>(compilation));
        var second = GetExtractedModel(TestHelper.GenerateTracked<InfrastructureGenerator>(compilation));
        await Assert.That(second).IsNotSameReferenceAs(first);
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task EditSource_SourceSensitiveSelectionIsNotReused()
    {
        var baseCompilation = CreateWithTwoTypeLibrary();
        var shadowedCompilation = baseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(ShadowingSource));

        // The shadowed run must not memoize its (source-dependent) choice for later compilations.
        var driver1 = TestHelper.GenerateTracked<InfrastructureGenerator>(shadowedCompilation);
        await Assert.That(GenerateInfrastructure(driver1)).Contains("typeof(global::OtherTestLibrary.SecondHooks)");

        var driver2 = driver1.RunGenerators(baseCompilation);
        await AssertRunReason(driver2, IncrementalStepRunReason.Modified);
        await Assert.That(GenerateInfrastructure(driver2)).Contains("typeof(global::OtherTestLibrary.FirstHooks)");
    }

    [Test]
    public async Task EditSource_ShadowingSelectedType_ShouldRegenerate()
    {
        var compilation1 = CreateWithTwoTypeLibrary();
        var driver1 = TestHelper.GenerateTracked<InfrastructureGenerator>(compilation1);
        await Assert.That(GenerateInfrastructure(driver1)).Contains("typeof(global::OtherTestLibrary.FirstHooks)");

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(ShadowingSource));
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReason(driver2, IncrementalStepRunReason.Modified);
        await Assert.That(GenerateInfrastructure(driver2)).Contains("typeof(global::OtherTestLibrary.SecondHooks)");

        var driver3 = driver2.RunGenerators(compilation1);
        await AssertRunReason(driver3, IncrementalStepRunReason.Modified);
        await Assert.That(GenerateInfrastructure(driver3)).Contains("typeof(global::OtherTestLibrary.FirstHooks)");
    }

    // The driver keeps the previous output instance whenever a rerun produces an equal model, so
    // whether the walk was skipped is only observable on the memo itself: it returns the same
    // instance on a hit and a new one after re-extracting.

    [Test]
    public async Task Memo_SyntaxOnlyEdits_ReuseModel()
    {
        var memo = new InfrastructureGenerator.AssemblyInfoMemo();
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));
        var model1 = memo.GetAssemblyInfo(compilation1);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        await Assert.That(memo.GetAssemblyInfo(compilation2)).IsSameReferenceAs(model1);

        var compilation3 = TestHelper.ReplaceMethodDeclaration(compilation1, "Test1",
            """
            [Test]
            public void Test1()
            {
                var x = 1;
            }
            """);
        await Assert.That(memo.GetAssemblyInfo(compilation3)).IsSameReferenceAs(model1);
    }

    [Test]
    public async Task Memo_ReferenceChange_ReExtracts()
    {
        var memo = new InfrastructureGenerator.AssemblyInfoMemo();
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));
        var model1 = memo.GetAssemblyInfo(compilation1);

        var compilation2 = compilation1.AddReferences(CreateHookLibrary(compilation1, "SharedHooks").ToMetadataReference());
        var model2 = memo.GetAssemblyInfo(compilation2);
        await Assert.That(model2).IsNotSameReferenceAs(model1);
        await Assert.That(model2).IsNotEqualTo(model1);
    }

    [Test]
    public async Task Memo_OptionsOrAssemblyNameChange_ReExtracts()
    {
        var memo = new InfrastructureGenerator.AssemblyInfoMemo();
        var baseCompilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));
        var baseModel = memo.GetAssemblyInfo(baseCompilation);

        // Same reference list, but options can change how references bind to assembly symbols,
        // so a new options instance must not reuse the model extracted under the old one.
        var withComparer = baseCompilation.WithOptions(
            baseCompilation.Options.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default));
        await Assert.That(baseCompilation.ExternalReferences == withComparer.ExternalReferences).IsTrue();
        var comparerModel = memo.GetAssemblyInfo(withComparer);
        await Assert.That(comparerModel).IsNotSameReferenceAs(baseModel);
        await Assert.That(comparerModel).IsEqualTo(baseModel);

        var withImportOptions = baseCompilation.WithOptions(
            baseCompilation.Options.WithMetadataImportOptions(MetadataImportOptions.All));
        var importModel = memo.GetAssemblyInfo(withImportOptions);
        await Assert.That(importModel).IsNotSameReferenceAs(comparerModel);

        // Syntax-only edits under the new options reuse the memo again.
        await Assert.That(memo.GetAssemblyInfo(
            withImportOptions.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}")))).IsSameReferenceAs(importModel);

        var renamed = withImportOptions.WithAssemblyName("Renamed");
        await Assert.That(withImportOptions.ExternalReferences == renamed.ExternalReferences).IsTrue();
        var renamedModel = memo.GetAssemblyInfo(renamed);
        await Assert.That(renamedModel).IsNotSameReferenceAs(importModel);
        await Assert.That(renamedModel.AssemblyName).IsEqualTo("Renamed");
    }

    [Test]
    public async Task Memo_ScriptCompilation_IsNeverMemoized()
    {
        var memo = new InfrastructureGenerator.AssemblyInfoMemo();
        var baseCompilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));
        var baseModel = memo.GetAssemblyInfo(baseCompilation);

        // Script submissions read inputs outside the memo key (#r directives, the previous
        // submission and the internal ReferencesSupersedeLowerVersions option), so they always
        // extract fresh.
        var script = CSharpCompilation.CreateScriptCompilation(
            baseCompilation.AssemblyName!,
            CSharpSyntaxTree.ParseText("var x = 1;", CSharpParseOptions.Default.WithKind(SourceCodeKind.Script)),
            baseCompilation.ExternalReferences);
        await Assert.That(baseCompilation.ExternalReferences == script.ExternalReferences).IsTrue();

        var first = memo.GetAssemblyInfo(script);
        var second = memo.GetAssemblyInfo(script.ReplaceSyntaxTree(
            script.SyntaxTrees.Single(),
            CSharpSyntaxTree.ParseText("var y = 2;", CSharpParseOptions.Default.WithKind(SourceCodeKind.Script))));
        await Assert.That(first).IsNotSameReferenceAs(baseModel);
        await Assert.That(second).IsNotSameReferenceAs(first);
    }

    private static CSharpCompilation CreateWithTwoTypeLibrary()
    {
        var consumer = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));
        var library = CSharpCompilation.Create(
            "OtherTestLibrary",
            [CSharpSyntaxTree.ParseText("namespace OtherTestLibrary { public class FirstHooks { [TUnit.Core.Before(TUnit.Core.HookType.Assembly)] public static void Setup() { } } public class SecondHooks { } }")],
            consumer.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return consumer.AddReferences(library.ToMetadataReference());
    }

    private static string GenerateInfrastructure(GeneratorDriver driver) =>
        driver.GetRunResult().GeneratedTrees.Single(t => t.FilePath.EndsWith("TUnitInfrastructure.g.cs")).ToString();

    private static CSharpCompilation CreateHookLibrary(Compilation consumer, string typeName) =>
        CSharpCompilation.Create(
            "OtherTestLibrary",
            [CSharpSyntaxTree.ParseText($"namespace OtherTestLibrary {{ public class {typeName} {{ [TUnit.Core.Before(TUnit.Core.HookType.Assembly)] public static void Setup() {{ }} }} }}")],
            consumer.References.Where(r => r is not CompilationReference),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static async Task AssertRunReason(GeneratorDriver driver, IncrementalStepRunReason reason)
    {
        var runResult = driver.GetRunResult().Results[0];

        await TestHelper.AssertRunReason(runResult, InfrastructureGenerator.ExtractAssemblyInfoStep, reason, 0);
    }
}