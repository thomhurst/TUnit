using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Process = System.Diagnostics.Process;

[MarkdownExporterAttribute.GitHub]
public class Benchmarks
{
    [Benchmark]
    public async Task TUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", "run")
        {
            WorkingDirectory = "../../../../TUnitTimer",
        })!.WaitForExitAsync();
    }

    [Benchmark]
    public async Task NUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", "test")
        {
            WorkingDirectory = "../../../../NUnitTimer",
        })!.WaitForExitAsync();
    }

    [Benchmark]
    public async Task xUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", "test")
        {
            WorkingDirectory = "../../../../xUnitTimer",
        })!.WaitForExitAsync();
    }
    
    [Benchmark]
    public async Task MSTest()
    {
        await Process.Start(new ProcessStartInfo("dotnet", "test")
        {
            WorkingDirectory = "../../../../MSTestTimer",
        })!.WaitForExitAsync();
    }
}