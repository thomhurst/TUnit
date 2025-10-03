using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using CliWrap;
using CliWrap.Buffered;

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
            .ExecuteBufferedAsync();
        await Cli.Wrap("dotnet")
            .WithArguments(["build", UnifiedPath, "-c", "Release", "-p:TestFramework=XUNIT", "--framework", Framework])
            .ExecuteBufferedAsync();
        await Cli.Wrap("dotnet")
            .WithArguments(["build", UnifiedPath, "-c", "Release", "-p:TestFramework=NUNIT", "--framework", Framework])
            .ExecuteBufferedAsync();
        await Cli.Wrap("dotnet")
            .WithArguments(["build", UnifiedPath, "-c", "Release", "-p:TestFramework=MSTEST", "--framework", Framework])
            .ExecuteBufferedAsync();

        // Publish AOT configuration
        var aotOutputPath = Path.Combine(UnifiedPath, "bin", "Release-TUNIT-AOT", Framework);
        await Cli.Wrap("dotnet")
            .WithArguments(["publish", UnifiedPath, "-c", "Release", "-p:TestFramework=TUNIT", "-p:Aot=true", "--framework", Framework, "--runtime", GetRuntimeIdentifier(), "--output", aotOutputPath])
            .ExecuteBufferedAsync();
    }

    private static string GetRuntimeIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-x64";
        }

        return "osx-arm64";
    }

    [Benchmark]
    [BenchmarkCategory("Runtime", "AOT")]
    public async Task TUnit_AOT()
    {
        var aotPath = Path.Combine(UnifiedPath, "bin", "Release-TUNIT-AOT", Framework);
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "UnifiedTests.exe" : "UnifiedTests";

        await Cli.Wrap(Path.Combine(aotPath, exeName))
            .WithArguments(["--treenode-filter",  $"/*/*/{ClassName}/*"])
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteBufferedAsync();
    }

    [Benchmark]
    public async Task TUnit()
    {
        var binPath = Path.Combine(UnifiedPath, "bin", "Release-TUNIT", Framework);
        var exeName = GetExecutableFileName();

        await Cli.Wrap(Path.Combine(binPath, exeName))
            .WithArguments(["--treenode-filter",  $"/*/*/{ClassName}/*"])
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteBufferedAsync();
    }

    [Benchmark]
    public async Task NUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["test", "--no-build", "-c", "Release", "-p:TestFramework=NUNIT", "--filter", $"FullyQualifiedName~{ClassName}", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteBufferedAsync();
    }

    [Benchmark]
    public async Task xUnit()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["test", "--no-build", "-c", "Release", "-p:TestFramework=XUNIT", "--filter", $"FullyQualifiedName~{ClassName}", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteBufferedAsync();
    }

    [Benchmark]
    public async Task MSTest()
    {
        await Cli.Wrap("dotnet")
            .WithArguments(["test", "--no-build", "-c", "Release", "-p:TestFramework=MSTEST", "--filter", $"FullyQualifiedName~{ClassName}", "--framework", Framework])
            .WithWorkingDirectory(UnifiedPath)
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteBufferedAsync();
    }
}
