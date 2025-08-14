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
            .WithArguments(["build", "--no-incremental", "-c", "Release", "--framework", Framework])
            .WithWorkingDirectory(UnitPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task Build_NUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "-c", "Release", "--framework", Framework])
            .WithWorkingDirectory(NUnitPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task Build_xUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "-c", "Release", "--framework", Framework])
            .WithWorkingDirectory(XUnitPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task Build_xUnitV3()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "-c", "Release", "--framework", Framework])
            .WithWorkingDirectory(XUnitV3Path)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task Build_MSTest()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["build", "--no-incremental", "-c", "Release", "--framework", Framework])
            .WithWorkingDirectory(MsTestPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
}
