using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
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

    [GlobalSetup(Target = nameof(Compile))]
    public void SetupCompile() =>
        (_sampleCompilation, _sampleDriver, _workspace) =
        WorkspaceHelper.SetupAsync<TestMetadataGenerator>(SampleProjectPath)
            .GetAwaiter()
            .GetResult();

    [Benchmark]
    public GeneratorDriver Compile() => _sampleDriver!.RunGeneratorsAndUpdateCompilation(_sampleCompilation!, out _, out _);

    [GlobalCleanup]
    public void Cleanup()
    {
        _workspace?.Dispose();
    }
}
