namespace TUnit.Engine.Configuration;

internal static class EnvironmentConstants
{
    // TUnit-specific: Reporters
    public const string DisableGithubReporter = "TUNIT_DISABLE_GITHUB_REPORTER";
    public const string DisableJUnitReporter = "TUNIT_DISABLE_JUNIT_REPORTER";
    public const string DisableHtmlReporter = "TUNIT_DISABLE_HTML_REPORTER";
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

    // GitHub Actions runtime (for artifact upload)
    public const string ActionsRuntimeToken = "ACTIONS_RUNTIME_TOKEN";
    public const string ActionsResultsUrl = "ACTIONS_RESULTS_URL";
    public const string GitHubRepository = "GITHUB_REPOSITORY";
    public const string GitHubRunId = "GITHUB_RUN_ID";

    // GitHub Actions context (for CI metadata in reports)
    public const string GitHubSha = "GITHUB_SHA";
    public const string GitHubRef = "GITHUB_REF";
    public const string GitHubHeadRef = "GITHUB_HEAD_REF";
    public const string GitHubEventName = "GITHUB_EVENT_NAME";
}
