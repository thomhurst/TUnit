using BenchmarkDotNet.Attributes;
using CliWrap;

namespace Tests.Benchmark;

[BenchmarkCategory("Build")]
public class BuildBenchmarks : BenchmarkBase
{
    // --disable-build-servers makes every build start without a warm compiler server or reusable
    // MSBuild node. With them, the first benchmark to run is slower for its first ~30 builds, so
    // the results depended on method order.
    [Benchmark]
    public async Task Build_TUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "--disable-build-servers", "-c", "Release", "-p:TestFramework=TUNIT", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task Build_NUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "--disable-build-servers", "-c", "Release", "-p:TestFramework=NUNIT", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task Build_MSTest()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "--disable-build-servers", "-c", "Release", "-p:TestFramework=MSTEST", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }

    [Benchmark]
    public async Task Build_xUnit3()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "--disable-build-servers", "-c", "Release", "-p:TestFramework=XUNIT3", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
}
