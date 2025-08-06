using System.Reflection;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Tests.Benchmark;

public class FrameworkVersionColumn : IColumn
{
    private static readonly Dictionary<string, string> VersionCache = new();
    
    public string Id => nameof(FrameworkVersionColumn);
    public string ColumnName => "Version";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Job;
    public int PriorityInCategory => 0;
    public bool IsNumeric => false;
    public UnitType UnitType => UnitType.Dimensionless;
    public string Legend => "Test Framework Version";

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        return GetValue(summary, benchmarkCase, SummaryStyle.Default);
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var methodName = benchmarkCase.Descriptor.WorkloadMethod.Name;
        return GetFrameworkVersion(methodName);
    }

    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    private static string GetFrameworkVersion(string methodName)
    {
        if (VersionCache.TryGetValue(methodName, out var cachedVersion))
        {
            return cachedVersion;
        }

        var version = methodName switch
        {
            "TUnit" or "TUnit_AOT" or "Build_TUnit" => GetTUnitVersion(),
            "xUnit" or "Build_xUnit" => GetXUnitVersion(),
            "NUnit" or "Build_NUnit" => GetNUnitVersion(),
            "MSTest" or "Build_MSTest" => GetMSTestVersion(),
            _ => "Unknown"
        };

        VersionCache[methodName] = version;
        return version;
    }

    private static string GetTUnitVersion()
    {
        try
        {
            // Try to get the version from the current codebase's Directory.Packages.props
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && directory.Name != "TUnit")
            {
                directory = directory.Parent;
            }

            if (directory != null)
            {
                var packagesPropsPath = Path.Combine(directory.FullName, "Directory.Packages.props");
                if (File.Exists(packagesPropsPath))
                {
                    var content = File.ReadAllText(packagesPropsPath);
                    var match = System.Text.RegularExpressions.Regex.Match(content, 
                        @"<PackageVersion\s+Include=""TUnit""\s+Version=""([^""]+)""");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }

            // Fallback: try to get from loaded assemblies
            var tunitAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "TUnit.Core");
            
            if (tunitAssembly != null)
            {
                var version = tunitAssembly.GetName().Version;
                if (version != null)
                {
                    return version.ToString();
                }
            }

            return "";
        }
        catch
        {
            return "";
        }
    }

    private static string GetXUnitVersion()
    {
        try
        {
            // Get from Directory.Packages.props
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && directory.Name != "TUnit")
            {
                directory = directory.Parent;
            }

            if (directory != null)
            {
                var packagesPropsPath = Path.Combine(directory.FullName, "Directory.Packages.props");
                if (File.Exists(packagesPropsPath))
                {
                    var content = File.ReadAllText(packagesPropsPath);
                    var match = System.Text.RegularExpressions.Regex.Match(content, 
                        @"<PackageVersion\s+Include=""xunit""\s+Version=""([^""]+)""");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }

            return "";
        }
        catch
        {
            return "";
        }
    }

    private static string GetNUnitVersion()
    {
        try
        {
            // Get from Directory.Packages.props
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && directory.Name != "TUnit")
            {
                directory = directory.Parent;
            }

            if (directory != null)
            {
                var packagesPropsPath = Path.Combine(directory.FullName, "Directory.Packages.props");
                if (File.Exists(packagesPropsPath))
                {
                    var content = File.ReadAllText(packagesPropsPath);
                    var match = System.Text.RegularExpressions.Regex.Match(content, 
                        @"<PackageVersion\s+Include=""NUnit""\s+Version=""([^""]+)""");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }

            return "";
        }
        catch
        {
            return "";
        }
    }

    private static string GetMSTestVersion()
    {
        try
        {
            // Get from Directory.Packages.props
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && directory.Name != "TUnit")
            {
                directory = directory.Parent;
            }

            if (directory != null)
            {
                var packagesPropsPath = Path.Combine(directory.FullName, "Directory.Packages.props");
                if (File.Exists(packagesPropsPath))
                {
                    var content = File.ReadAllText(packagesPropsPath);
                    var match = System.Text.RegularExpressions.Regex.Match(content, 
                        @"<PackageVersion\s+Include=""MSTest\.TestFramework""\s+Version=""([^""]+)""");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }

            return "";
        }
        catch
        {
            return "";
        }
    }
}