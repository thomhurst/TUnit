using BenchmarkDotNet.Attributes;
using CliWrap;

namespace Tests.Benchmark;

[BenchmarkCategory("Runtime")]
public class RuntimeBenchmarks : BenchmarkBase
{
    private static readonly string? ClassName = Environment.GetEnvironmentVariable("CLASS_NAME");
    
    [Benchmark]
    [BenchmarkCategory("Runtime")]
    public async Task TUnit_AOT()
    {
        await Cli.Wrap(Path.Combine(UnitPath, $"aot-publish-{Framework.Replace(".0", "")}", GetExecutableFileName()))
            .WithArguments(["--treenode-filter",  $"/*/*/{ClassName}/*"])
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }

    [Benchmark]
    public async Task TUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["run", "--no-build", "-c", "Release", "--treenode-filter",  $"/*/*/{ClassName}/*", "--framework", Framework])
            .WithWorkingDirectory(UnitPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }

    [Benchmark]
    public async Task NUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["test", "--no-build", "-c", "Release", "--filter", $"FullyQualifiedName~{ClassName}", "--framework", Framework])
            .WithWorkingDirectory(NUnitPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }

    [Benchmark]
    public async Task xUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["test", "--no-build", "-c", "Release", "--filter", $"FullyQualifiedName~{ClassName}", "--framework", Framework])
            .WithWorkingDirectory(XUnitPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }

    [Benchmark]
    public async Task MSTest()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["test", "--no-build", "-c", "Release", "--filter", $"FullyQualifiedName~{ClassName}", "--framework", Framework])
            .WithWorkingDirectory(MsTestPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
}
