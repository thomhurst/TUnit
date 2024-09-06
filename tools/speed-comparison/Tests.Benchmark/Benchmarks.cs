using System.Diagnostics;
using System.Runtime.InteropServices;
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
    
    private static readonly string? ClassName = Environment.GetEnvironmentVariable("CLASS_NAME");

    [Benchmark]
    public async Task TUnit_AOT()
    {
        await Process.Start(new ProcessStartInfo(GetExecutableFileName(), $"--treenode-filter /*/*/{ClassName}/*")
        {
            WorkingDirectory = Path.Combine(TUnitPath, "aot-publish"),
        })!.WaitForExitAsync();
    }

    [Benchmark]
    public async Task TUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", $"run --no-build -c Release --treenode-filter /*/*/{ClassName}/*")
        {
            WorkingDirectory = TUnitPath,
        })!.WaitForExitAsync();
    }

    [Benchmark]
    public async Task NUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", $"test --no-build -c Release --filter FullyQualifiedName~{ClassName}")
        {
            WorkingDirectory = NUnitPath,
        })!.WaitForExitAsync();
    }

    [Benchmark]
    public async Task xUnit()
    {
        await Process.Start(new ProcessStartInfo("dotnet", $"test --no-build -c Release --filter FullyQualifiedName~{ClassName}")
        {
            WorkingDirectory = xUnitPath,
        })!.WaitForExitAsync();
    }

    [Benchmark]
    public async Task MSTest()
    {
        await Process.Start(new ProcessStartInfo("dotnet", $"test --no-build -c Release --filter FullyQualifiedName~{ClassName}")
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

    private string GetPlatformFolder()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-x64";
        }
        
        throw new NotImplementedException();
    }

    private string GetExecutableFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "TUnit.TestProject.exe";
        }

        return "TUnit.TestProject";
    }
}
