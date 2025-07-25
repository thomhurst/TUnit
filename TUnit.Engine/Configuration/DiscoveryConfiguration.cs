using TUnit.Engine.Services;

namespace TUnit.Engine.Configuration;

/// <summary>
/// Configuration settings for test discovery with intelligent resource-based safeguards
/// </summary>
public static class DiscoveryConfiguration
{
    // Cache environment variables at static initialization to avoid repeated lookups
    private static readonly string? _cachedDiscoveryDiagnosticsEnvVar = Environment.GetEnvironmentVariable("TUNIT_DISCOVERY_DIAGNOSTICS");
    private static readonly string? _cachedDiscoveryTimeoutEnvVar = Environment.GetEnvironmentVariable("TUNIT_DISCOVERY_TIMEOUT_SECONDS");
    private static readonly string? _cachedDataSourceTimeoutEnvVar = Environment.GetEnvironmentVariable("TUNIT_DATA_SOURCE_TIMEOUT_SECONDS");
    
    // Cache CI environment variables at startup
    private static readonly string?[] _cachedCiEnvVars =
    [
        Environment.GetEnvironmentVariable("CI"),
        Environment.GetEnvironmentVariable("CONTINUOUS_INTEGRATION"),
        Environment.GetEnvironmentVariable("BUILD_ID"),
        Environment.GetEnvironmentVariable("BUILD_NUMBER"),
        Environment.GetEnvironmentVariable("GITHUB_ACTIONS"),
        Environment.GetEnvironmentVariable("GITLAB_CI"),
        Environment.GetEnvironmentVariable("AZURE_PIPELINES"),
        Environment.GetEnvironmentVariable("JENKINS_URL"),
        Environment.GetEnvironmentVariable("TEAMCITY_VERSION"),
        Environment.GetEnvironmentVariable("APPVEYOR"),
        Environment.GetEnvironmentVariable("CIRCLECI"),
        Environment.GetEnvironmentVariable("TRAVIS")
    ];
    
    // Cache container environment variables at startup
    private static readonly string?[] _cachedContainerEnvVars =
    [
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
        Environment.GetEnvironmentVariable("CONTAINER"),
        Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST")
    ];

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
    public static bool EnableDiagnostics { get; set; } = _cachedDiscoveryDiagnosticsEnvVar == "1";

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
        
        // Check if running in CI (CI environments get longer timeouts)
        var isCI = IsRunningInCI();
        var ciScaling = isCI ? 2.0 : 1.0;
        
        // Check if running in container (containers might be resource-constrained)
        var isContainer = IsRunningInContainer();
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
        var isCI = IsRunningInCI();
        var ciScaling = isCI ? 3.0 : 1.0; // CI environments often have slower I/O
        
        var isContainer = IsRunningInContainer();
        var containerScaling = isContainer ? 2.0 : 1.0;
        
        var totalScaling = ciScaling * containerScaling;
        var scaledTimeout = TimeSpan.FromMilliseconds(baseTimeout.TotalMilliseconds * totalScaling);
        
        // Cap between 10 seconds and 10 minutes
        return TimeSpan.FromMilliseconds(Math.Max(10000, Math.Min(600000, scaledTimeout.TotalMilliseconds)));
    }

    private static bool IsRunningInCI()
    {
        // Use cached environment variables instead of repeated lookups
        for (var i = 0; i < _cachedCiEnvVars.Length; i++)
        {
            if (!string.IsNullOrEmpty(_cachedCiEnvVars[i]))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsRunningInContainer()
    {
        // Use cached environment variables instead of repeated lookups
        for (var i = 0; i < _cachedContainerEnvVars.Length; i++)
        {
            if (!string.IsNullOrEmpty(_cachedContainerEnvVars[i]))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Configures discovery settings from environment variables (simplified)
    /// </summary>
    public static void ConfigureFromEnvironment()
    {
        if (int.TryParse(_cachedDiscoveryTimeoutEnvVar, out var timeoutSec))
        {
            DiscoveryTimeout = TimeSpan.FromSeconds(timeoutSec);
        }

        if (int.TryParse(_cachedDataSourceTimeoutEnvVar, out var dataTimeoutSec))
        {
            DataSourceTimeout = TimeSpan.FromSeconds(dataTimeoutSec);
        }
    }
}
