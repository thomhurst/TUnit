using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Helpers;
using LogLevel = TUnit.Core.Logging.LogLevel;

#pragma warning disable TPEXP

namespace TUnit.Engine.Services;

/// <summary>
/// Centralized service for managing TUnit output and diagnostic settings
/// </summary>
public sealed class VerbosityService
{
    private readonly bool _isDetailedOutput;
    private readonly LogLevel _logLevel;

    public VerbosityService(ICommandLineOptions commandLineOptions, IServiceProvider serviceProvider)
    {
        _isDetailedOutput = GetOutputLevel(commandLineOptions, serviceProvider);
        _logLevel = GetLogLevel(commandLineOptions);
    }

    /// <summary>
    /// Whether to show detailed stack traces (enabled with Debug/Trace log level)
    /// </summary>
    public bool ShowDetailedStackTrace => _logLevel <= LogLevel.Debug;

    /// <summary>
    /// Whether to hide real-time test output (hidden with --output Normal)
    /// </summary>
    public bool HideTestOutput => !_isDetailedOutput;

    /// <summary>
    /// Whether to show the TUnit logo
    /// </summary>
    public bool ShowLogo => true;

    /// <summary>
    /// Whether to enable discovery diagnostics (enabled with Debug/Trace log level)
    /// </summary>
    public bool EnableDiscoveryDiagnostics => _logLevel <= LogLevel.Debug;

    /// <summary>
    /// Whether to enable verbose source generator diagnostics (enabled with Debug/Trace log level)
    /// </summary>
    public bool EnableVerboseSourceGeneratorDiagnostics => _logLevel <= LogLevel.Debug;

    /// <summary>
    /// Whether to show execution timing details (enabled with Debug/Trace log level)
    /// </summary>
    public bool ShowExecutionTiming => _logLevel <= LogLevel.Debug;

    /// <summary>
    /// Whether to show parallel execution details (enabled with Debug/Trace log level)
    /// </summary>
    public bool ShowParallelExecutionDetails => _logLevel <= LogLevel.Debug;

    /// <summary>
    /// Whether to show test discovery progress (enabled with Debug/Trace log level)
    /// </summary>
    public bool ShowDiscoveryProgress => _logLevel <= LogLevel.Debug;

    /// <summary>
    /// Whether to show memory and resource usage (enabled with Debug/Trace log level)
    /// </summary>
    public bool ShowResourceUsage => _logLevel <= LogLevel.Debug;

    /// <summary>
    /// Creates a summary of current output and diagnostic settings
    /// </summary>
    public string CreateVerbositySummary()
    {
        var outputMode = _isDetailedOutput ? "Detailed" : "Normal";
        return $"Output: {outputMode}, Log Level: {_logLevel} " +
               $"(Stack traces: {ShowDetailedStackTrace}, " +
               $"Discovery diagnostics: {EnableDiscoveryDiagnostics})";
    }

    // Use centralized environment variable cache

    private static bool GetOutputLevel(ICommandLineOptions commandLineOptions, IServiceProvider serviceProvider)
    {
        // Check for --output flag (Microsoft.Testing.Platform extension)
        if (commandLineOptions.TryGetOptionArgumentList("output", out var args) && args.Length > 0)
        {
            return args[0].Equals("Detailed", StringComparison.OrdinalIgnoreCase);
        }

        // Smart defaults: Normal for console (buffered output), Detailed for IDE (real-time output)
        return !IsConsoleEnvironment(serviceProvider);
    }

    private static LogLevel GetLogLevel(ICommandLineOptions commandLineOptions)
    {
        // Check for --log-level flag
        if (commandLineOptions.TryGetOptionArgumentList(LogLevelCommandProvider.LogLevelOption, out var args) && args.Length > 0)
        {
            return LogLevelCommandProvider.ParseLogLevel(args);
        }

        // Check cached legacy environment variable for backwards compatibility
        if (EnvironmentVariableCache.Get("TUNIT_DISCOVERY_DIAGNOSTICS") == "1")
        {
            return LogLevel.Debug;
        }

        return LogLevel.Information;
    }

    private static bool IsConsoleEnvironment(IServiceProvider serviceProvider)
    {
        try
        {
            var clientInfo = serviceProvider.GetClientInfo();
            return clientInfo.Id.Contains("console", StringComparison.InvariantCultureIgnoreCase);
        }
        catch
        {
            // If we can't determine, default to console behavior
            return true;
        }
    }
}