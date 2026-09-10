using TUnit.Engine.Configuration;

namespace TUnit.Engine.Reporters;

/// <summary>
/// URL templates for a detected source-control provider. Server, repository slug and
/// commit are already baked in; the placeholders <c>{path}</c>, <c>{line}</c>,
/// <c>{start}</c> and <c>{end}</c> are filled per test by the report's client script.
/// </summary>
/// <param name="LineUrl">Blob link to a single line (the "Jump to source" button).</param>
/// <param name="RangeUrl">Blob link to a line range, for tests with a known end line.</param>
/// <param name="RawUrl">
/// Raw-content URL for the inline snippet. Set only for providers whose raw endpoint
/// allows cross-origin <c>fetch</c> from the static report (GitHub.com, Bitbucket Cloud);
/// <see langword="null"/> means link-only (e.g. GitLab raw sends no CORS header).
/// </param>
internal sealed record SourceLinkTemplates(string LineUrl, string RangeUrl, string? RawUrl);

/// <summary>
/// CI / source-control context resolved from environment variables: enough to render
/// report metadata and, when a supported provider is detected, source-link templates.
/// </summary>
internal sealed record SourceControlContext(
    string? CommitSha,
    string? Branch,
    string? PullRequestNumber,
    string? RepositorySlug,
    string? Workspace,
    SourceLinkTemplates? Links)
{
    public static readonly SourceControlContext Empty = new(null, null, null, null, null, null);

    /// <summary>
    /// Detects the active provider from <paramref name="getEnv"/> and builds its context.
    /// Providers are probed by their unique sentinel variable; the first match wins.
    /// </summary>
    public static SourceControlContext Detect(Func<string, string?> getEnv)
    {
        if (getEnv(EnvironmentConstants.GitHubActions) is "true")
        {
            return BuildGitHub(getEnv);
        }

        if (getEnv(EnvironmentConstants.GitLabCi) is "true")
        {
            return BuildGitLab(getEnv);
        }

        if (!string.IsNullOrEmpty(getEnv(EnvironmentConstants.BitbucketBuildNumber)))
        {
            return BuildBitbucket(getEnv);
        }

        return Empty;
    }

    private static SourceControlContext BuildGitHub(Func<string, string?> getEnv)
    {
        var sha = getEnv(EnvironmentConstants.GitHubSha);
        var slug = getEnv(EnvironmentConstants.GitHubRepository);
        var server = getEnv(EnvironmentConstants.GitHubServerUrl)?.TrimEnd('/');
        var workspace = getEnv(EnvironmentConstants.GitHubWorkspace);

        // Branch: prefer GITHUB_HEAD_REF (set on PRs), fall back to GITHUB_REF (strip refs/heads/).
        var branch = getEnv(EnvironmentConstants.GitHubHeadRef);
        var gitRef = getEnv(EnvironmentConstants.GitHubRef);
        if (string.IsNullOrEmpty(branch) && gitRef is not null && gitRef.StartsWith("refs/heads/", StringComparison.Ordinal))
        {
            branch = gitRef["refs/heads/".Length..];
        }

        // PR number: parse from GITHUB_REF when it matches refs/pull/{n}/merge.
        string? prNumber = null;
        if (gitRef is not null && gitRef.StartsWith("refs/pull/", StringComparison.Ordinal) && gitRef.EndsWith("/merge", StringComparison.Ordinal))
        {
            prNumber = gitRef["refs/pull/".Length..^"/merge".Length];
        }

        SourceLinkTemplates? links = null;
        if (!string.IsNullOrEmpty(server) && !string.IsNullOrEmpty(slug) && !string.IsNullOrEmpty(sha))
        {
            var blob = $"{server}/{slug}/blob/{sha}/{{path}}";
            // raw.githubusercontent.com is the only CORS-enabled raw host; Enterprise raw is link-only.
            var raw = server == EnvironmentConstants.GitHubDefaultServerUrl
                ? $"https://raw.githubusercontent.com/{slug}/{sha}/{{path}}"
                : null;
            links = new SourceLinkTemplates($"{blob}#L{{line}}", $"{blob}#L{{start}}-L{{end}}", raw);
        }

        return new SourceControlContext(sha, branch, prNumber, slug, workspace, links);
    }

    private static SourceControlContext BuildGitLab(Func<string, string?> getEnv)
    {
        var sha = getEnv(EnvironmentConstants.GitLabCommitSha);
        var slug = getEnv(EnvironmentConstants.GitLabProjectPath);
        var server = getEnv(EnvironmentConstants.GitLabServerUrl)?.TrimEnd('/');
        var workspace = getEnv(EnvironmentConstants.GitLabProjectDir);
        var branch = getEnv(EnvironmentConstants.GitLabBranch);

        SourceLinkTemplates? links = null;
        if (!string.IsNullOrEmpty(server) && !string.IsNullOrEmpty(slug) && !string.IsNullOrEmpty(sha))
        {
            var blob = $"{server}/{slug}/-/blob/{sha}/{{path}}";
            // GitLab's raw endpoint sends no Access-Control-Allow-Origin, so the in-report
            // fetch is blocked — link only, no inline snippet.
            links = new SourceLinkTemplates($"{blob}#L{{line}}", $"{blob}#L{{start}}-{{end}}", null);
        }

        return new SourceControlContext(sha, branch, null, slug, workspace, links);
    }

    private static SourceControlContext BuildBitbucket(Func<string, string?> getEnv)
    {
        var sha = getEnv(EnvironmentConstants.BitbucketCommit);
        var slug = getEnv(EnvironmentConstants.BitbucketRepoFullName);
        var workspace = getEnv(EnvironmentConstants.BitbucketCloneDir);
        var branch = getEnv(EnvironmentConstants.BitbucketBranch);
        const string server = EnvironmentConstants.BitbucketServerUrl;

        SourceLinkTemplates? links = null;
        if (!string.IsNullOrEmpty(slug) && !string.IsNullOrEmpty(sha))
        {
            var blob = $"{server}/{slug}/src/{sha}/{{path}}";
            // Bitbucket Cloud's raw endpoint sends Access-Control-Allow-Origin: *, so snippets work.
            var raw = $"{server}/{slug}/raw/{sha}/{{path}}";
            links = new SourceLinkTemplates($"{blob}#lines-{{line}}", $"{blob}#lines-{{start}}:{{end}}", raw);
        }

        return new SourceControlContext(sha, branch, null, slug, workspace, links);
    }
}
