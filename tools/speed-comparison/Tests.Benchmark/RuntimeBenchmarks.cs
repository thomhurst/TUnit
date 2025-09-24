using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using CliWrap;

namespace Tests.Benchmark;

[BenchmarkCategory("Runtime")]
public class RuntimeBenchmarks : BenchmarkBase
{
    private static readonly string? ClassName = Environment.GetEnvironmentVariable("CLASS_NAME");

    [GlobalSetup]
    public async Task Setup()
    {
        // Build all framework configurations
        await Cli.Wrap("dotnet")
            .WithArguments(["build", UnifiedPath, "-c", "Release", "-p:TestFramework=TUNIT", "--framework", Framework])
            .ExecuteAsync();
        await Cli.Wrap("dotnet")
            .WithArguments(["build", UnifiedPath, "-c", "Release", "-p:TestFramework=XUNIT", "--framework", Framework])
            .ExecuteAsync();
        await Cli.Wrap("dotnet")
            .WithArguments(["build", UnifiedPath, "-c", "Release", "-p:TestFramework=NUNIT", "--framework", Framework])
            .ExecuteAsync();
        await Cli.Wrap("dotnet")
            .WithArguments(["build", UnifiedPath, "-c", "Release", "-p:TestFramework=MSTEST", "--framework", Framework])
            .ExecuteAsync();
    }
    
    [Benchmark]
    [BenchmarkCategory("Runtime", "AOT")]
    public async Task TUnit_AOT()
    {
        // Note: AOT build must be done separately with: dotnet publish -c Release -p:TestFramework=TUNIT -p:PublishAot=true
        var aotPath = Path.Combine(UnifiedPath, "bin", "Release", Framework, "publish");
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "UnifiedTests.exe" : "UnifiedTests";
        
        await Cli.Wrap(Path.Combine(aotPath, exeName))
            .WithArguments(["--treenode-filter",  $"/*/*/{ClassName}/*"])
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }

    [Benchmark]
    public async Task TUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["run", "--no-build", "-c", "Release", "-p:TestFramework=TUNIT", "--treenode-filter",  $"/*/*/{ClassName}/*", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }

    [Benchmark]
    public async Task NUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["test", "--no-build", "-c", "Release", "-p:TestFramework=NUNIT", "--filter", $"FullyQualifiedName~{ClassName}", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }

    [Benchmark]
    public async Task xUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["test", "--no-build", "-c", "Release", "-p:TestFramework=XUNIT", "--filter", $"FullyQualifiedName~{ClassName}", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }

    [Benchmark]
    public async Task MSTest()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["test", "--no-build", "-c", "Release", "-p:TestFramework=MSTEST", "--filter", $"FullyQualifiedName~{ClassName}", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteAsync();
    }
}
