using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Tests.Benchmark;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkBase
{
    protected readonly Stream OutputStream = Console.OpenStandardOutput();

    protected static readonly string UnitPath = GetProjectPath("TUnitTimer");
    protected static readonly string NUnitPath = GetProjectPath("NUnitTimer");
    protected static readonly string XUnitPath = GetProjectPath("xUnitTimer");
    protected static readonly string MsTestPath = GetProjectPath("MSTestTimer");
    
    protected static readonly string Framework = GetFramework();

    private static string GetFramework()
    {
        return $"net{Environment.Version.Major}.{Environment.Version.Minor}";
    }

    [GlobalCleanup]
    public async Task FlushStream()
    {
        await OutputStream.FlushAsync();
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

    protected string GetExecutableFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "TUnitTimer.exe";
        }

        return "TUnitTimer";
    }
}
