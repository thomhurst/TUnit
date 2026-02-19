namespace TUnit.Engine.Configuration;

internal static class EnvironmentConstants
{
    // TUnit-specific: Reporters
    public const string DisableGithubReporter = "TUNIT_DISABLE_GITHUB_REPORTER";
    public const string DisableJUnitReporter = "TUNIT_DISABLE_JUNIT_REPORTER";
    public const string EnableJUnitReporter = "TUNIT_ENABLE_JUNIT_REPORTER";
    public const string GitHubReporterStyle = "TUNIT_GITHUB_REPORTER_STYLE";

    // TUnit-specific: Execution
    public const string ExecutionMode = "TUNIT_EXECUTION_MODE";
    public const string MaxParallelTests = "TUNIT_MAX_PARALLEL_TESTS";

    // TUnit-specific: Display and diagnostics
    public const string DisableLogo = "TUNIT_DISABLE_LOGO";
    public const string EnableIdeStreaming = "TUNIT_ENABLE_IDE_STREAMING";
    public const string DiscoveryDiagnostics = "TUNIT_DISCOVERY_DIAGNOSTICS";

    // TUnit-specific: JUnit output
    public const string JUnitXmlOutputPath = "JUNIT_XML_OUTPUT_PATH";

    // Legacy/deprecated (kept for backwards compatibility)
    public const string DisableGithubReporterLegacy = "DISABLE_GITHUB_REPORTER";

    // External CI environment variables
    public const string GitHubActions = "GITHUB_ACTIONS";
    public const string GitHubStepSummary = "GITHUB_STEP_SUMMARY";
    public const string GitLabCi = "GITLAB_CI";
    public const string CiServer = "CI_SERVER";
}
