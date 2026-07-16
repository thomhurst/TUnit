using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Configuration;
using LogLevel = TUnit.Core.Logging.LogLevel;

#pragma warning disable TPEXP

namespace TUnit.Engine.Services;

/// <summary>
/// Centralized service for managing TUnit output and diagnostic settings.
/// Controls whether output goes to console vs real-time streaming, and manages
/// stack trace verbosity based on log levels and command-line options.
/// </summary>
public sealed class VerbosityService
{
    private readonly bool _isDetailedOutput;
    private readonly LogLevel _logLevel;

    public VerbosityService(ICommandLineOptions commandLineOptions, IServiceProvider serviceProvider)
    {
        _isDetailedOutput = GetOutputLevel(commandLineOptions, serviceProvider);
        _logLevel = GetLogLevel(commandLineOptions);
        IsIdeClient = !IsConsoleEnvironment(serviceProvider);
    }

    /// <summary>
    /// Whether running in an IDE (Rider, VS, etc.) vs console.
    /// </summary>
    public bool IsIdeClient { get; }

    /// <summary>
    /// Whether to show detailed stack traces (enabled with Debug/Trace log level)
    /// </summary>
    public bool ShowDetailedStackTrace => _logLevel <= LogLevel.Debug;

    /// <summary>
    /// Whether detailed output mode is enabled (--output Detailed)
    /// </summary>
    public bool IsDetailedOutput => _isDetailedOutput;

    /// <summary>
    /// Whether to hide real-time test output from the console.
    /// For IDE clients, we hide console output because we stream via TestNodeUpdateMessage instead.
    /// For console clients, we hide if --output Normal and log level is not Debug/Trace.
    /// </summary>
    public bool HideTestOutput => IsIdeClient || (!_isDetailedOutput && _logLevel > LogLevel.Debug);

    /// <summary>
    /// Creates a summary of current output and diagnostic settings.
    /// </summary>
    public string CreateVerbositySummary()
    {
        var outputMode = _isDetailedOutput ? "Detailed" : "Normal";
        var clientType = IsIdeClient ? "IDE" : "Console";
        return $"Output: {outputMode}, Log Level: {_logLevel}, Client: {clientType} " +
               $"(Stack traces: {ShowDetailedStackTrace}, Hide output: {HideTestOutput})";
    }

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

        // Check legacy environment variable for backwards compatibility
        if (Environment.GetEnvironmentVariable(EnvironmentConstants.DiscoveryDiagnostics) == "1")
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
        catch (Exception ex)
        {
            // If we can't determine, default to console behavior
            System.Diagnostics.Debug.WriteLine($"Failed to determine console environment: {ex}");
            return true;
        }
    }
}
