namespace TUnit.Core.Configuration;

/// <summary>
/// Configuration for TUnit runtime behavior.
/// </summary>
public static class TUnitConfiguration
{
    /// <summary>
    /// When true, enables diagnostic output for TestBuilder operations.
    /// </summary>
    public static bool EnableDiagnostics { get; set; } = GetDiagnosticsSetting();
    
    private static bool GetDiagnosticsSetting()
    {
        var envVar = Environment.GetEnvironmentVariable("TUNIT_TESTBUILDER_DIAGNOSTICS");
        return bool.TryParse(envVar, out var enabled) && enabled;
    }
}