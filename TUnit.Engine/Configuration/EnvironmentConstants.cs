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
    public const string MaxOtelExternalSpans = "TUNIT_OTEL_MAX_EXTERNAL_SPANS";

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
    public const string GitHubWorkspace = "GITHUB_WORKSPACE";
    public const string GitHubServerUrl = "GITHUB_SERVER_URL";

    // Default GitHub server (overridden by GITHUB_SERVER_URL on GitHub Enterprise)
    public const string GitHubDefaultServerUrl = "https://github.com";

    // GitLab CI context (for source links in reports)
    public const string GitLabServerUrl = "CI_SERVER_URL";
    public const string GitLabProjectPath = "CI_PROJECT_PATH";
    public const string GitLabCommitSha = "CI_COMMIT_SHA";
    public const string GitLabProjectDir = "CI_PROJECT_DIR";
    public const string GitLabBranch = "CI_COMMIT_REF_NAME";

    // Bitbucket Pipelines context (for source links in reports)
    public const string BitbucketBuildNumber = "BITBUCKET_BUILD_NUMBER";
    public const string BitbucketRepoFullName = "BITBUCKET_REPO_FULL_NAME";
    public const string BitbucketCommit = "BITBUCKET_COMMIT";
    public const string BitbucketCloneDir = "BITBUCKET_CLONE_DIR";
    public const string BitbucketBranch = "BITBUCKET_BRANCH";
    public const string BitbucketServerUrl = "https://bitbucket.org";
}
