using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Configuration;

/// <summary>
/// Configuration settings for test discovery with intelligent resource-based safeguards
/// </summary>
public static class DiscoveryConfiguration
{
    // Use centralized environment variable cache instead of individual static fields

    /// <summary>
    /// Maximum time allowed for overall test discovery (auto-scaled based on system)
    /// </summary>
    public static TimeSpan DiscoveryTimeout { get; set; } = GetIntelligentDiscoveryTimeout();

    /// <summary>
    /// Maximum time allowed for dynamic data source resolution (auto-scaled based on system)
    /// </summary>
    public static TimeSpan DataSourceTimeout { get; set; } = GetIntelligentDataSourceTimeout();

    /// <summary>
    /// Whether to enable discovery diagnostics (default: environment variable TUNIT_DISCOVERY_DIAGNOSTICS)
    /// </summary>
    public static bool EnableDiagnostics { get; set; } = EnvironmentVariableCache.Get("TUNIT_DISCOVERY_DIAGNOSTICS") == "1";

    /// <summary>
    /// Creates an intelligent circuit breaker for discovery operations
    /// </summary>
    public static DiscoveryCircuitBreaker CreateCircuitBreaker()
    {
        return new DiscoveryCircuitBreaker();
    }

    /// <summary>
    /// Gets intelligent discovery timeout based on system resources
    /// </summary>
    private static TimeSpan GetIntelligentDiscoveryTimeout()
    {
        var baseTimeout = TimeSpan.FromSeconds(30);
        
        // Scale based on CPU count (more cores = potentially more complex projects)
        var cpuScaling = Math.Max(1.0, Environment.ProcessorCount / 4.0);
        
        var isCI = EnvironmentVariableCache.IsRunningInCI();
        var ciScaling = isCI ? 2.0 : 1.0;
        
        var isContainer = EnvironmentVariableCache.IsRunningInContainer();
        var containerScaling = isContainer ? 1.5 : 1.0;
        
        var totalScaling = cpuScaling * ciScaling * containerScaling;
        var scaledTimeout = TimeSpan.FromMilliseconds(baseTimeout.TotalMilliseconds * totalScaling);
        
        // Cap between 15 seconds and 5 minutes
        return TimeSpan.FromMilliseconds(Math.Max(15000, Math.Min(300000, scaledTimeout.TotalMilliseconds)));
    }

    /// <summary>
    /// Gets intelligent data source timeout based on system resources
    /// </summary>
    private static TimeSpan GetIntelligentDataSourceTimeout()
    {
        var baseTimeout = TimeSpan.FromSeconds(30);
        
        // Data source operations are often I/O bound, so scale differently
        var isCI = EnvironmentVariableCache.IsRunningInCI();
        var ciScaling = isCI ? 3.0 : 1.0; // CI environments often have slower I/O
        
        var isContainer = EnvironmentVariableCache.IsRunningInContainer();
        var containerScaling = isContainer ? 2.0 : 1.0;
        
        var totalScaling = ciScaling * containerScaling;
        var scaledTimeout = TimeSpan.FromMilliseconds(baseTimeout.TotalMilliseconds * totalScaling);
        
        // Cap between 10 seconds and 10 minutes
        return TimeSpan.FromMilliseconds(Math.Max(10000, Math.Min(600000, scaledTimeout.TotalMilliseconds)));
    }


    /// <summary>
    /// Configures discovery settings from environment variables (simplified)
    /// </summary>
    public static void ConfigureFromEnvironment()
    {
        if (int.TryParse(EnvironmentVariableCache.Get("TUNIT_DISCOVERY_TIMEOUT_SECONDS"), out var timeoutSec))
        {
            DiscoveryTimeout = TimeSpan.FromSeconds(timeoutSec);
        }

        if (int.TryParse(EnvironmentVariableCache.Get("TUNIT_DATA_SOURCE_TIMEOUT_SECONDS"), out var dataTimeoutSec))
        {
            DataSourceTimeout = TimeSpan.FromSeconds(dataTimeoutSec);
        }
    }
}
