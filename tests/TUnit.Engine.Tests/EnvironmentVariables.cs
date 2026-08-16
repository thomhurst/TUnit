namespace TUnit.Engine.Tests;

public class EnvironmentVariables
{
    public static readonly string? NetVersion = Environment.GetEnvironmentVariable("NET_VERSION");

    public static readonly bool IsNetFramework = NetVersion?.StartsWith("net4") == true;

    // Keep the report for the outer TUnit.Engine.Tests suite, but do not generate or upload
    // reports for the short-lived test applications that it launches as implementation details.
    public static Dictionary<string, string?> DisableHtmlReporterForChildProcess() => new()
    {
        ["TUNIT_DISABLE_HTML_REPORTER"] = "true"
    };
}
