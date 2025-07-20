using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TUnit.Engine.Services;

/// <summary>
/// Automatically detects optimal parallelism settings based on system resources
/// </summary>
public static class ParallelismDetector
{
    /// <summary>
    /// Detects optimal parallelism for test execution based on system capabilities
    /// </summary>
    /// <returns>Optimal number of parallel threads</returns>
    public static int DetectOptimalParallelism()
    {
        var processorCount = Environment.ProcessorCount;
        var availableMemoryGb = GetAvailableMemoryGB();
        var isContainerEnvironment = IsRunningInContainer();
        var isCiEnvironment = IsRunningInCI();

        // Base parallelism on processor count
        var baseParallelism = processorCount;

        // Adjust for memory constraints
        if (availableMemoryGb < 2.0)
        {
            // Low memory systems - be conservative
            baseParallelism = Math.Max(1, processorCount / 2);
        }
        else if (availableMemoryGb > 8.0)
        {
            // High memory systems - can handle more parallel work
            baseParallelism = processorCount * 2;
        }

        // Container environments often have CPU limits that don't match processor count
        if (isContainerEnvironment)
        {
            baseParallelism = Math.Min(baseParallelism, Math.Max(2, processorCount));
        }

        // CI environments benefit from slightly higher parallelism for I/O bound operations
        if (isCiEnvironment && !isContainerEnvironment)
        {
            baseParallelism = Math.Min(baseParallelism * 3 / 2, processorCount * 3);
        }

        // Ensure minimum of 1 and reasonable maximum
        return Math.Max(1, Math.Min(baseParallelism, processorCount * 4));
    }

    /// <summary>
    /// Gets available memory in GB (approximate)
    /// </summary>
    private static double GetAvailableMemoryGB()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsAvailableMemoryGB();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxAvailableMemoryGB();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOSAvailableMemoryGB();
            }
        }
        catch
        {
            // Fall back to conservative estimate
        }

        // Default conservative estimate: assume 4GB if detection fails
        return 4.0;
    }

    private static double GetWindowsAvailableMemoryGB()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "OS get TotalVisibleMemorySize /value",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        foreach (var line in output.Split('\n'))
        {
            if (line.StartsWith("TotalVisibleMemorySize="))
            {
                var memoryKB = line.Split('=')[1].Trim();
                if (long.TryParse(memoryKB, out var kb))
                {
                    return kb / (1024.0 * 1024.0); // Convert KB to GB
                }
            }
        }

        return 4.0; // Default fallback
    }

    private static double GetLinuxAvailableMemoryGB()
    {
        try
        {
            var memInfo = File.ReadAllText("/proc/meminfo");
            foreach (var line in memInfo.Split('\n'))
            {
                if (line.StartsWith("MemAvailable:"))
                {
                    var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && long.TryParse(parts[1], out var kb))
                    {
                        return kb / (1024.0 * 1024.0); // Convert KB to GB
                    }
                }
            }
        }
        catch
        {
            // Fall back to reading MemTotal if MemAvailable not available
            try
            {
                var memInfo = File.ReadAllText("/proc/meminfo");
                foreach (var line in memInfo.Split('\n'))
                {
                    if (line.StartsWith("MemTotal:"))
                    {
                        var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out var kb))
                        {
                            return (kb * 0.8) / (1024.0 * 1024.0); // Assume 80% available
                        }
                    }
                }
            }
            catch
            {
                // Ignore and use default
            }
        }

        return 4.0; // Default fallback
    }

    private static double GetMacOSAvailableMemoryGB()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sysctl",
                    Arguments = "-n hw.memsize",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (long.TryParse(output, out var bytes))
            {
                return bytes / (1024.0 * 1024.0 * 1024.0); // Convert bytes to GB
            }
        }
        catch
        {
            // Fall back to default
        }

        return 4.0; // Default fallback
    }

    /// <summary>
    /// Detects if running in a container environment
    /// </summary>
    private static bool IsRunningInContainer()
    {
        // Check common container environment indicators
        var containerEnvVars = new[] { "DOTNET_RUNNING_IN_CONTAINER", "CONTAINER", "KUBERNETES_SERVICE_HOST" };
        
        foreach (var envVar in containerEnvVars)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
            {
                return true;
            }
        }

        // Check for container-specific files on Linux
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                if (File.Exists("/.dockerenv"))
                {
                    return true;
                }

                var cgroup = File.ReadAllText("/proc/1/cgroup");
                if (cgroup.Contains("docker") || cgroup.Contains("kubepods") || cgroup.Contains("containerd"))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore errors checking container files
            }
        }

        return false;
    }

    /// <summary>
    /// Detects if running in a CI environment
    /// </summary>
    private static bool IsRunningInCI()
    {
        var ciEnvVars = new[] 
        { 
            "CI", "CONTINUOUS_INTEGRATION", "BUILD_ID", "BUILD_NUMBER",
            "GITHUB_ACTIONS", "GITLAB_CI", "AZURE_PIPELINES", "JENKINS_URL",
            "TEAMCITY_VERSION", "APPVEYOR", "CIRCLECI", "TRAVIS"
        };

        return ciEnvVars.Any(envVar => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)));
    }
}