using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Process = System.Diagnostics.Process;

namespace Tests.Benchmark;

[MarkdownExporterAttribute.GitHub]
public class Benchmarks
{
    private static readonly string TUnitPath = GetProjectPath("TUnitTimer");
    private static readonly string NUnitPath = GetProjectPath("NUnitTimer");
    private static readonly string xUnitPath = GetProjectPath("xUnitTimer");
    private static readonly string MSTestPath = GetProjectPath("MSTestTimer");
    
    [Benchmark]
    public async Task TUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", $"run --no-build -c Release --treenode-filter /*/*/{Environment.GetEnvironmentVariable("CLASS_NAME")}/*")
        {
            WorkingDirectory = TUnitPath,
        })!.WaitForExitAsync();
    }

    [Benchmark]
    public async Task NUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", $"test --no-build -c Release --filter FullyQualifiedName~{Environment.GetEnvironmentVariable("CLASS_NAME")}")
        {
            WorkingDirectory = NUnitPath,
        })!.WaitForExitAsync();
    }

    [Benchmark]
    public async Task xUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", $"test --no-build -c Release --filter FullyQualifiedName~{Environment.GetEnvironmentVariable("CLASS_NAME")}")
        {
            WorkingDirectory = xUnitPath,
        })!.WaitForExitAsync();
    }
    
    [Benchmark]
    public async Task MSTest()
    {
        await Process.Start(new ProcessStartInfo("dotnet", $"test --no-build -c Release --filter FullyQualifiedName~{Environment.GetEnvironmentVariable("CLASS_NAME")}")
        {
            WorkingDirectory = MSTestPath,
        })!.WaitForExitAsync();
    }
    
    private static string GetProjectPath(string name)
    {
        var folder = new DirectoryInfo(Environment.CurrentDirectory);

        while (folder.Name != "speed-comparison")
        {
            folder = folder.Parent!;
        }
        
        return Path.Combine(folder.FullName, name, name);
    }
}