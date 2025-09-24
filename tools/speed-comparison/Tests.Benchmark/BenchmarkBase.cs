using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Tests.Benchmark;

[Config(typeof(BenchmarkConfig))]
public class BenchmarkBase
{
    protected readonly Stream OutputStream = Console.OpenStandardOutput();

    protected static readonly string UnifiedPath = GetProjectPath("UnifiedTests");
    
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
        
        return Path.Combine(folder.FullName, name);
    }

    protected string GetExecutableFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "UnifiedTests.exe";
        }

        return "UnifiedTests";
    }
}
