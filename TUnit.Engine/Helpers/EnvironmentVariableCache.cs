using System.Collections.Concurrent;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Centralized cache for environment variables to avoid repeated system calls
/// Initializes all environment variables on first access and caches them for the lifetime of the application
/// </summary>
internal static class EnvironmentVariableCache
{
    private static readonly ConcurrentDictionary<string, string?> _cache = new();
    private static readonly object _initLock = new();
    private static bool _initialized = false;

    /// <summary>
    /// All environment variable keys that TUnit cares about
    /// This helps us cache only the variables we need rather than all environment variables
    /// </summary>
    private static readonly string[] _tunitEnvironmentVariables =
    [
        // TUnit specific variables
        "TUNIT_DISCOVERY_DIAGNOSTICS",
        "TUNIT_DISCOVERY_TIMEOUT_SECONDS", 
        "TUNIT_DATA_SOURCE_TIMEOUT_SECONDS",
        "TUNIT_EXECUTION_MODE",
        "TUNIT_ADAPTIVE_MIN_PARALLELISM",
        "TUNIT_ADAPTIVE_MAX_PARALLELISM", 
        "TUNIT_ADAPTIVE_METRICS",
        "TUNIT_DISABLE_GITHUB_REPORTER",
        
        // CI environment detection variables
        "CI",
        "CONTINUOUS_INTEGRATION",
        "BUILD_ID",
        "BUILD_NUMBER",
        "GITHUB_ACTIONS",
        "GITLAB_CI",
        "AZURE_PIPELINES", 
        "JENKINS_URL",
        "TEAMCITY_VERSION",
        "APPVEYOR",
        "CIRCLECI",
        "TRAVIS",
        
        // Container detection variables
        "DOTNET_RUNNING_IN_CONTAINER",
        "CONTAINER",
        "KUBERNETES_SERVICE_HOST",
        
        // GitHub specific variables
        "DISABLE_GITHUB_REPORTER",
        "GITHUB_STEP_SUMMARY"
    ];

    /// <summary>
    /// Gets the cached value of an environment variable
    /// Initializes the cache on first call
    /// </summary>
    /// <param name="variableName">The name of the environment variable</param>
    /// <returns>The environment variable value or null if not set</returns>
    public static string? Get(string variableName)
    {
        EnsureInitialized();
        _cache.TryGetValue(variableName, out var value);
        return value;
    }

    /// <summary>
    /// Gets multiple cached environment variable values
    /// Useful for checking multiple CI or container detection variables
    /// </summary>
    /// <param name="variableNames">The names of the environment variables to retrieve</param>
    /// <returns>An array of environment variable values (nulls for unset variables)</returns>
    public static string?[] GetMultiple(params string[] variableNames)
    {
        EnsureInitialized();
        var result = new string?[variableNames.Length];
        for (var i = 0; i < variableNames.Length; i++)
        {
            _cache.TryGetValue(variableNames[i], out var value);
            result[i] = value;
        }
        return result;
    }

    /// <summary>
    /// Checks if any of the specified environment variables have non-empty values
    /// Useful for CI and container detection
    /// </summary>
    /// <param name="variableNames">The environment variable names to check</param>
    /// <returns>True if any of the variables have non-empty values</returns>
    public static bool HasAnyNonEmpty(params string[] variableNames)
    {
        EnsureInitialized();
        for (var i = 0; i < variableNames.Length; i++)
        {
            _cache.TryGetValue(variableNames[i], out var value);
            if (!string.IsNullOrEmpty(value))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// CI environment variables for quick access
    /// </summary>
    public static readonly string[] CiVariables =
    [
        "CI",
        "CONTINUOUS_INTEGRATION", 
        "BUILD_ID",
        "BUILD_NUMBER",
        "GITHUB_ACTIONS",
        "GITLAB_CI",
        "AZURE_PIPELINES",
        "JENKINS_URL", 
        "TEAMCITY_VERSION",
        "APPVEYOR",
        "CIRCLECI",
        "TRAVIS"
    ];

    /// <summary>
    /// Container environment variables for quick access
    /// </summary>
    public static readonly string[] ContainerVariables =
    [
        "DOTNET_RUNNING_IN_CONTAINER",
        "CONTAINER",
        "KUBERNETES_SERVICE_HOST"
    ];

    /// <summary>
    /// Convenience method to check if running in a CI environment
    /// </summary>
    public static bool IsRunningInCI()
    {
        return HasAnyNonEmpty(CiVariables);
    }

    /// <summary>
    /// Convenience method to check if running in a container
    /// </summary>
    public static bool IsRunningInContainer()
    {
        return HasAnyNonEmpty(ContainerVariables);
    }

    /// <summary>
    /// Initializes the cache with all TUnit environment variables
    /// Thread-safe and only runs once
    /// </summary>
    private static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (_initLock)
        {
            if (_initialized)
            {
                return;
            }

            // Cache all TUnit-related environment variables
            foreach (var variableName in _tunitEnvironmentVariables)
            {
                var value = Environment.GetEnvironmentVariable(variableName);
                _cache.TryAdd(variableName, value);
            }

            _initialized = true;
        }
    }

    /// <summary>
    /// For testing purposes - allows clearing and reinitializing the cache
    /// </summary>
    internal static void ClearCache()
    {
        lock (_initLock)
        {
            _cache.Clear();
            _initialized = false;
        }
    }
}