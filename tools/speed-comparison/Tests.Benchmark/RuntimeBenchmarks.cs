using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using CliWrap;
using CliWrap.Buffered;

namespace Tests.Benchmark;

[BenchmarkCategory("Runtime")]
public class RuntimeBenchmarks : BenchmarkBase
{
    private static readonly string? ClassName = Environment.GetEnvironmentVariable("CLASS_NAME");

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
        var dllPath = Path.Combine(UnifiedPath, "bin", "Release-NUNIT", Framework, "UnifiedTests.dll");

        await Cli.Wrap("dotnet")
            .WithArguments(["test", dllPath, "--filter", $"FullyQualifiedName~{ClassName}"])
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteBufferedAsync();
    }

    [Benchmark]
    public async Task xUnit()
    {
        var dllPath = Path.Combine(UnifiedPath, "bin", "Release-XUNIT", Framework, "UnifiedTests.dll");

        await Cli.Wrap("dotnet")
            .WithArguments(["test", dllPath, "--filter", $"FullyQualifiedName~{ClassName}"])
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteBufferedAsync();
    }

    [Benchmark]
    public async Task MSTest()
    {
        var dllPath = Path.Combine(UnifiedPath, "bin", "Release-MSTEST", Framework, "UnifiedTests.dll");

        await Cli.Wrap("dotnet")
            .WithArguments(["test", dllPath, "--filter", $"FullyQualifiedName~{ClassName}"])
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteBufferedAsync();
    }

    [Benchmark]
    public async Task xUnit3()
    {
        var binPath = Path.Combine(UnifiedPath, "bin", "Release-XUNIT3", Framework);
        var exeName = GetExecutableFileName();

        await Cli.Wrap(Path.Combine(binPath, exeName))
            .WithArguments(["--filter", $"/*/*/{ClassName}/*"])
            .WithStandardOutputPipe(PipeTarget.ToStream(OutputStream))
            .ExecuteBufferedAsync();
    }
}
