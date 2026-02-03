using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.SourceGenerator.Benchmarks;

[MemoryDiagnoser]
[InProcess]
public class HookMetadataGeneratorBenchmarks
{
    private const string SampleProjectPath = "../TUnit.TestProject/TUnit.TestProject.csproj";

    private MSBuildWorkspace? _workspace;
    private GeneratorDriver? _driver;
    private Compilation? _compilation;

    [GlobalSetup(Target = nameof(RunGenerator))]
    public void SetupRunGenerator() =>
        (_compilation, _driver, _workspace) =
        WorkspaceHelper.SetupAsync<HookMetadataGenerator>(SampleProjectPath)
            .GetAwaiter()
            .GetResult();

    [Benchmark]
    public GeneratorDriver RunGenerator() => _driver!.RunGeneratorsAndUpdateCompilation(_compilation!, out _, out _);

    [GlobalCleanup]
    public void Cleanup()
    {
        _workspace?.Dispose();
    }
}
