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

    [Fact]
    public void EditSource_ShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<InfrastructureGenerator>(compilation1);
        AssertRunReason(driver1, IncrementalStepRunReason.New);
        var model1 = GetExtractedModel(driver1);

        // Syntax-only edits rerun the cheap Select, which returns the memoized model (same instance,
        // so the reference walk was skipped) and keeps the generated source cached.
        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReason(driver2, IncrementalStepRunReason.Unchanged);
        Xunit.Assert.Same(model1, GetExtractedModel(driver2));
        TestHelper.AssertSourceOutputsCached(driver2.GetRunResult().Results[0]);

        var compilation3 = TestHelper.ReplaceMethodDeclaration(compilation1, "Test1",
            """
            [Test]
            public void Test1()
            {
                var x = 1;
            }
            """);
        var driver3 = driver2.RunGenerators(compilation3);
        AssertRunReason(driver3, IncrementalStepRunReason.Unchanged);
        Xunit.Assert.Same(model1, GetExtractedModel(driver3));
        TestHelper.AssertSourceOutputsCached(driver3.GetRunResult().Results[0]);
    }

    [Fact]
    public void EditSource_DoesNotRetainPreviousCompilation()
    {
        var (driver, firstCompilation, latestCompilation) = RunSourceEdits(10);

        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // A comparer that reports the new compilation as equal makes the input node keep the old
        // one, pinning its syntax trees and bound state for as long as the driver lives.
        Xunit.Assert.False(firstCompilation.IsAlive, "The generator driver kept the first compilation alive after syntax-only edits.");
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

    [Fact]
    public void EditProjectReference_ShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var library1 = CreateHookLibrary(compilation1, "SharedHooks");
        compilation1 = compilation1.AddReferences(library1.ToMetadataReference());

        var driver1 = TestHelper.GenerateTracked<InfrastructureGenerator>(compilation1);
        AssertRunReason(driver1, IncrementalStepRunReason.New);

        // In the IDE, editing a referenced project replaces its CompilationReference while the
        // assembly name stays the same. Renaming the only public type must reach the output,
        // otherwise TUnitInfrastructure.g.cs keeps a typeof() to a type that no longer exists.
        var library2 = CreateHookLibrary(compilation1, "RenamedHooks");
        var compilation2 = compilation1.ReplaceReference(compilation1.References.Last(), library2.ToMetadataReference());
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReason(driver2, IncrementalStepRunReason.Modified);

        var generated = driver2.GetRunResult().GeneratedTrees.Single(t => t.FilePath.EndsWith("TUnitInfrastructure.g.cs")).ToString();
        Xunit.Assert.Contains("global::OtherTestLibrary.RenamedHooks", generated);
        Xunit.Assert.DoesNotContain("global::OtherTestLibrary.SharedHooks", generated);

        // An edit that does not change the extracted model reruns extraction but keeps the output cached.
        var library3 = CreateHookLibrary(compilation1, "RenamedHooks");
        var compilation3 = compilation2.ReplaceReference(compilation2.References.Last(), library3.ToMetadataReference());
        var driver3 = driver2.RunGenerators(compilation3);
        AssertRunReason(driver3, IncrementalStepRunReason.Unchanged);
    }

    private const string ShadowingSource = "namespace OtherTestLibrary { public class FirstHooks { } }";

    [Fact]
    public void FreshDriver_SameReferences_SourceShadowsSelectedType_SelectsNextType()
    {
        var baseCompilation = CreateWithTwoTypeLibrary();

        // Populate the shared memo from an unrelated driver.
        var unshadowed = GenerateInfrastructure(CSharpGeneratorDriver.Create(new InfrastructureGenerator()).RunGenerators(baseCompilation));
        Xunit.Assert.Contains("typeof(global::OtherTestLibrary.FirstHooks)", unshadowed);

        // Same reference array, but a source type now takes FirstHooks' name, so typeof() would bind
        // to the source type and never initialize OtherTestLibrary.
        var shadowedCompilation = baseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(ShadowingSource));
        var shadowed = GenerateInfrastructure(CSharpGeneratorDriver.Create(new InfrastructureGenerator()).RunGenerators(shadowedCompilation));
        Xunit.Assert.Contains("typeof(global::OtherTestLibrary.SecondHooks)", shadowed);
        Xunit.Assert.DoesNotContain("typeof(global::OtherTestLibrary.FirstHooks)", shadowed);
    }

    [Fact]
    public void FreshDriver_SameReferences_SourceSensitiveSelectionIsNotReused()
    {
        var baseCompilation = CreateWithTwoTypeLibrary();
        var shadowedCompilation = baseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(ShadowingSource));

        // The shadowed run must not memoize its (source-dependent) choice for later compilations.
        var shadowed = GenerateInfrastructure(CSharpGeneratorDriver.Create(new InfrastructureGenerator()).RunGenerators(shadowedCompilation));
        Xunit.Assert.Contains("typeof(global::OtherTestLibrary.SecondHooks)", shadowed);

        var unshadowed = GenerateInfrastructure(CSharpGeneratorDriver.Create(new InfrastructureGenerator()).RunGenerators(baseCompilation));
        Xunit.Assert.Contains("typeof(global::OtherTestLibrary.FirstHooks)", unshadowed);
    }

    [Fact]
    public void EditSource_ShadowingSelectedType_ShouldRegenerate()
    {
        var compilation1 = CreateWithTwoTypeLibrary();
        var driver1 = TestHelper.GenerateTracked<InfrastructureGenerator>(compilation1);
        Xunit.Assert.Contains("typeof(global::OtherTestLibrary.FirstHooks)", GenerateInfrastructure(driver1));

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(ShadowingSource));
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReason(driver2, IncrementalStepRunReason.Modified);
        Xunit.Assert.Contains("typeof(global::OtherTestLibrary.SecondHooks)", GenerateInfrastructure(driver2));

        var driver3 = driver2.RunGenerators(compilation1);
        AssertRunReason(driver3, IncrementalStepRunReason.Modified);
        Xunit.Assert.Contains("typeof(global::OtherTestLibrary.FirstHooks)", GenerateInfrastructure(driver3));
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

    private static void AssertRunReason(GeneratorDriver driver, IncrementalStepRunReason reason)
    {
        var runResult = driver.GetRunResult().Results[0];

        TestHelper.AssertRunReason(runResult, InfrastructureGenerator.ExtractAssemblyInfoStep, reason, 0);
    }
}
