namespace TUnit.Engine.Logging;

/// <summary>
/// Unified verbosity levels for TUnit output and diagnostics
/// </summary>
public enum TUnitVerbosity
{
    /// <summary>
    /// Minimal output - errors and critical information only
    /// </summary>
    Minimal = 0,
    
    /// <summary>
    /// Normal output - standard test results and basic information (default)
    /// </summary>
    Normal = 1,
    
    /// <summary>
    /// Verbose output - detailed execution information and timing
    /// </summary>
    Verbose = 2,
    
    /// <summary>
    /// Debug output - internal diagnostics, discovery details, and all framework information
    /// </summary>
    Debug = 3
}

/// <summary>
/// Extensions for working with TUnit verbosity levels
/// </summary>
public static class TUnitVerbosityExtensions
{
    /// <summary>
    /// Checks if the current verbosity level includes the specified level
    /// </summary>
    public static bool Includes(this TUnitVerbosity current, TUnitVerbosity level)
    {
        return current >= level;
    }
    
    /// <summary>
    /// Converts verbosity to human-readable string
    /// </summary>
    public static string ToDisplayString(this TUnitVerbosity verbosity)
    {
        return verbosity switch
        {
            TUnitVerbosity.Minimal => "Minimal",
            TUnitVerbosity.Normal => "Normal",
            TUnitVerbosity.Verbose => "Verbose", 
            TUnitVerbosity.Debug => "Debug",
            _ => verbosity.ToString()
        };
    }
}