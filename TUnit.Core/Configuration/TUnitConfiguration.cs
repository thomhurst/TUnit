namespace TUnit.Core.Configuration;

/// <summary>
/// Configuration for TUnit runtime behavior.
/// </summary>
public static class TUnitConfiguration
{
    /// <summary>
    /// The TestBuilder implementation to use.
    /// </summary>
    public static TestBuilderMode TestBuilderMode { get; set; } = GetTestBuilderMode();
    
    /// <summary>
    /// When true, enables diagnostic output for TestBuilder operations.
    /// </summary>
    public static bool EnableDiagnostics { get; set; } = GetDiagnosticsSetting();
    
    private static TestBuilderMode GetTestBuilderMode()
    {
        var modeStr = Environment.GetEnvironmentVariable("TUNIT_TESTBUILDER_MODE");
        if (Enum.TryParse<TestBuilderMode>(modeStr, true, out var mode))
        {
            return mode;
        }
        
        // Default to optimized version
        return TestBuilderMode.Optimized;
    }
    
    private static bool GetDiagnosticsSetting()
    {
        var envVar = Environment.GetEnvironmentVariable("TUNIT_TESTBUILDER_DIAGNOSTICS");
        return bool.TryParse(envVar, out var enabled) && enabled;
    }
}

/// <summary>
/// TestBuilder implementation modes.
/// </summary>
public enum TestBuilderMode
{
    /// <summary>
    /// Basic implementation without optimizations.
    /// </summary>
    Basic,
    
    /// <summary>
    /// Optimized implementation with caching and expression compilation.
    /// </summary>
    Optimized,
    
    /// <summary>
    /// Implementation with full diagnostic tracking.
    /// </summary>
    WithDiagnostics
}