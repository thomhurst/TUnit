namespace TUnit.Engine.Configuration;

/// <summary>
/// Configuration settings for test discovery with safeguards against hanging
/// </summary>
public static class DiscoveryConfiguration
{
    /// <summary>
    /// Maximum time allowed for overall test discovery (default: 30 seconds)
    /// </summary>
    public static TimeSpan DiscoveryTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum time allowed for dynamic data source resolution (default: 30 seconds)
    /// </summary>
    public static TimeSpan DataSourceTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum recursion depth for cartesian product generation (default: 100)
    /// </summary>
    public static int MaxCartesianDepth { get; set; } = 100;

    /// <summary>
    /// Maximum total combinations from cartesian product (default: 100,000)
    /// </summary>
    public static int MaxCartesianCombinations { get; set; } = 100_000;

    /// <summary>
    /// Maximum total tests per discovery session (default: 50,000)
    /// </summary>
    public static int MaxTestsPerDiscovery { get; set; } = 50_000;

    /// <summary>
    /// Maximum items allowed from a single data source (default: 10,000)
    /// </summary>
    public static int MaxDataSourceItems { get; set; } = 10_000;

    /// <summary>
    /// Whether to enable discovery diagnostics (default: environment variable TUNIT_DISCOVERY_DIAGNOSTICS)
    /// </summary>
    public static bool EnableDiagnostics { get; set; } = Environment.GetEnvironmentVariable("TUNIT_DISCOVERY_DIAGNOSTICS") == "1";

    /// <summary>
    /// Whether to log warnings when approaching limits (default: true)
    /// </summary>
    public static bool LogWarnings { get; set; } = true;

    /// <summary>
    /// Configures discovery settings from environment variables
    /// </summary>
    public static void ConfigureFromEnvironment()
    {
        if (int.TryParse(Environment.GetEnvironmentVariable("TUNIT_DISCOVERY_TIMEOUT_SECONDS"), out var timeoutSec))
        {
            DiscoveryTimeout = TimeSpan.FromSeconds(timeoutSec);
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("TUNIT_DATA_SOURCE_TIMEOUT_SECONDS"), out var dataTimeoutSec))
        {
            DataSourceTimeout = TimeSpan.FromSeconds(dataTimeoutSec);
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("TUNIT_MAX_TESTS"), out var maxTests))
        {
            MaxTestsPerDiscovery = maxTests;
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("TUNIT_MAX_COMBINATIONS"), out var maxCombinations))
        {
            MaxCartesianCombinations = maxCombinations;
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("TUNIT_MAX_DATA_ITEMS"), out var maxDataItems))
        {
            MaxDataSourceItems = maxDataItems;
        }
    }
}
