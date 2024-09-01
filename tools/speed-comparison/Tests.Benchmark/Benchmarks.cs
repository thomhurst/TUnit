using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Process = System.Diagnostics.Process;

[MarkdownExporterAttribute.GitHub]
public class Benchmarks
{
    private static string GetProjectPath(string name) =>
        Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", name);

    private static string TUnitPath = GetProjectPath("TUnitTimer");
    private static string NUnitPath = GetProjectPath("NUnitTimer");
    private static string xUnitPath = GetProjectPath("xUnitTimer");
    private static string MSTestPath = GetProjectPath("MSTestTimer");
    
    [Benchmark]
    public async Task TUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", "run")
        {
            WorkingDirectory = TUnitPath,
        })!.WaitForExitAsync();
    }

    [Benchmark]
    public async Task NUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", "test")
        {
            WorkingDirectory = NUnitPath,
        })!.WaitForExitAsync();
    }

    [Benchmark]
    public async Task xUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", "test")
        {
            WorkingDirectory = xUnitPath,
        })!.WaitForExitAsync();
    }
    
    [Benchmark]
    public async Task MSTest()
    {
        await Process.Start(new ProcessStartInfo("dotnet", "test")
        {
            WorkingDirectory = MSTestPath,
        })!.WaitForExitAsync();
    }
}