using System.Reflection;

namespace TUnit.Engine.Tests;

public class EnvironmentVariables
{
    public static readonly string? NetVersion = Environment.GetEnvironmentVariable("NET_VERSION");

    public static readonly bool IsNetFramework = NetVersion?.StartsWith("net4") == true;

    // The launched test applications are built as project references of this project, so they share its
    // configuration: Release in CI, Debug when an IDE builds and runs the solution.
    public static readonly string BuildConfiguration =
        typeof(EnvironmentVariables).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Release";

    // Keep the report for the outer TUnit.Engine.Tests suite, but do not generate or upload
    // reports for the short-lived test applications that it launches as implementation details.
    public static Dictionary<string, string?> DisableHtmlReporterForChildProcess() => new()
    {
        ["TUNIT_DISABLE_HTML_REPORTER"] = "true"
    };
}
