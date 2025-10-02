using BenchmarkDotNet.Attributes;
using CliWrap;

namespace Tests.Benchmark;

[BenchmarkCategory("Build")]
public class BuildBenchmarks : BenchmarkBase
{
    [Benchmark]
    public async Task Build_TUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "-c", "Release", "-p:TestFramework=TUNIT", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task Build_NUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "-c", "Release", "-p:TestFramework=NUNIT", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task Build_xUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "-c", "Release", "-p:TestFramework=XUNIT", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task Build_MSTest()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "-c", "Release", "-p:TestFramework=MSTEST", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
}
