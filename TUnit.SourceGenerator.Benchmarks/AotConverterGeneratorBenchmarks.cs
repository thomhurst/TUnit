using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.SourceGenerator.Benchmarks;

[MemoryDiagnoser]
[InProcess]
public class AotConverterGeneratorBenchmarks
{
    private const string SampleProjectPath = "../TUnit.TestProject/TUnit.TestProject.csproj";

    private MSBuildWorkspace? _workspace;
    private GeneratorDriver? _sampleDriver;
    private Compilation? _sampleCompilation;

    [GlobalSetup(Target = nameof(RunGenerator))]
    public void SetupRunGenerator() =>
        (_sampleCompilation, _sampleDriver, _workspace) =
        WorkspaceHelper.SetupAsync<AotConverterGenerator>(SampleProjectPath)
            .GetAwaiter()
            .GetResult();

    [Benchmark]
    public GeneratorDriver RunGenerator() => _sampleDriver!.RunGeneratorsAndUpdateCompilation(_sampleCompilation!, out _, out _);

    [GlobalCleanup]
    public void Cleanup()
    {
        _workspace?.Dispose();
    }
}
