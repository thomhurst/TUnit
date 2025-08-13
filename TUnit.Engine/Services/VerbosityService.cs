using Microsoft.Testing.Platform.CommandLine;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Helpers;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

/// <summary>
/// Centralized service for managing TUnit output verbosity and diagnostic settings
/// </summary>
public sealed class VerbosityService
{
    private readonly TUnitVerbosity _verbosity;

    public VerbosityService(ICommandLineOptions commandLineOptions)
    {
        _verbosity = GetVerbosityFromCommandLine(commandLineOptions);
    }

    /// <summary>
    /// Current verbosity level
    /// </summary>
    public TUnitVerbosity CurrentVerbosity => _verbosity;

    /// <summary>
    /// Whether to show detailed stack traces (Verbose+ levels)
    /// </summary>
    public bool ShowDetailedStackTrace => _verbosity.Includes(TUnitVerbosity.Verbose);

    /// <summary>
    /// Whether to hide test output (Minimal level only)
    /// </summary>
    public bool HideTestOutput => _verbosity == TUnitVerbosity.Minimal;

    /// <summary>
    /// Whether to show the TUnit logo (Normal+ levels)
    /// </summary>
    public bool ShowLogo => _verbosity.Includes(TUnitVerbosity.Normal);

    /// <summary>
    /// Whether to enable discovery diagnostics (Debug level only)
    /// </summary>
    public bool EnableDiscoveryDiagnostics => _verbosity.Includes(TUnitVerbosity.Debug);

    /// <summary>
    /// Whether to enable verbose source generator diagnostics (Debug level only)
    /// </summary>
    public bool EnableVerboseSourceGeneratorDiagnostics => _verbosity.Includes(TUnitVerbosity.Debug);

    /// <summary>
    /// Whether to show execution timing details (Verbose+ levels)
    /// </summary>
    public bool ShowExecutionTiming => _verbosity.Includes(TUnitVerbosity.Verbose);

    /// <summary>
    /// Whether to show parallel execution details (Debug level only)
    /// </summary>
    public bool ShowParallelExecutionDetails => _verbosity.Includes(TUnitVerbosity.Debug);

    /// <summary>
    /// Whether to show test discovery progress (Verbose+ levels)
    /// </summary>
    public bool ShowDiscoveryProgress => _verbosity.Includes(TUnitVerbosity.Verbose);

    /// <summary>
    /// Whether to show memory and resource usage (Debug level only)
    /// </summary>
    public bool ShowResourceUsage => _verbosity.Includes(TUnitVerbosity.Debug);

    /// <summary>
    /// Creates a summary of current verbosity settings
    /// </summary>
    public string CreateVerbositySummary()
    {
        return $"Verbosity: {_verbosity.ToDisplayString()} " +
               $"(Stack traces: {ShowDetailedStackTrace}, " +
               $"Logo: {ShowLogo}, " +
               $"Discovery diagnostics: {EnableDiscoveryDiagnostics})";
    }

    // Use centralized environment variable cache
    
    private static TUnitVerbosity GetVerbosityFromCommandLine(ICommandLineOptions commandLineOptions)
    {
        if (commandLineOptions.TryGetOptionArgumentList(VerbosityCommandProvider.Verbosity, out var args) && args.Length > 0)
        {
            return VerbosityCommandProvider.ParseVerbosity(args);
        }

        // Check cached legacy environment variable for backwards compatibility
        if (EnvironmentVariableCache.Get("TUNIT_DISCOVERY_DIAGNOSTICS") == "1")
        {
            return TUnitVerbosity.Debug;
        }

        return TUnitVerbosity.Normal;
    }
}