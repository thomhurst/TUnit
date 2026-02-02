using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using TUnit.Core.SourceGenerator.Generators;
using TUnit.SourceGenerator.Benchmarks;

namespace TUnit.SourceGenerator.Benchmarks;

[MemoryDiagnoser]
[InProcess]
public class TestMetadataGeneratorBenchmarks
{
    private const string SampleProjectPath = "../TUnit.TestProject/TUnit.TestProject.csproj";

    private MSBuildWorkspace? _workspace;
    private GeneratorDriver? _sampleDriver;
    private Compilation? _sampleCompilation;

    [GlobalSetup(Target = nameof(RunGenerator))]
    public void SetupRunGenerator() =>
        (_sampleCompilation, _sampleDriver, _workspace) =
        WorkspaceHelper.SetupAsync<TestMetadataGenerator>(SampleProjectPath)
            .GetAwaiter()
            .GetResult();

    // [IterationSetup(Target = nameof(RunGenerator))]
    // public void Setup()
    // {
    //     _sampleCompilation = _sampleCompilation!.AddSyntaxTrees([CSharpSyntaxTree.ParseText($"struct MyValue{Random.Shared.Next()} {{}}", options: (CSharpParseOptions)_sampleCompilation.SyntaxTrees.First().Options)]);
    //
    // }

    // [Benchmark]
    // public GeneratorDriver RunGenerator() => _sampleDriver!.RunGeneratorsAndUpdateCompilation(_sampleCompilation!, out _, out _);

    [Benchmark]
    public GeneratorDriver RunGenerator()
    {
        var driver = _sampleDriver!.RunGeneratorsAndUpdateCompilation(_sampleCompilation!, out _, out _);

        // add a random type to create a new compilation.
        _sampleCompilation = _sampleCompilation.AddSyntaxTrees([CSharpSyntaxTree.ParseText($"struct MyValue{Random.Shared.Next()} {{}}", options: (CSharpParseOptions)_sampleCompilation.SyntaxTrees.First().Options)]);
        return driver;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _workspace?.Dispose();
    }
}
