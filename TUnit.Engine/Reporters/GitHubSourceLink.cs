using System.Text.RegularExpressions;

namespace TUnit.Engine.Reporters;

/// <summary>
/// Shared helpers for turning a local test source file path into a GitHub
/// repository-relative path, used by both the GitHub and HTML reporters.
/// </summary>
internal static partial class GitHubSourceLink
{
    // Deterministic builds (ContinuousIntegrationBuild=true, which CI enables) remap each
    // SourceRoot to "/_/", "/_1/", "/_2/", ... via PathMap, so test file paths look like
    // "/_/TUnit.Engine/Foo.cs" — they no longer contain the real workspace or repo name.
    // The remainder after the prefix is already the repo-relative path.
#if NET
    [GeneratedRegex(@"^/_\d*/")]
    private static partial Regex DeterministicRootRegex();
#else
    private static readonly Regex _deterministicRootRegex = new(@"^/_\d*/", RegexOptions.Compiled);
    private static Regex DeterministicRootRegex() => _deterministicRootRegex;
#endif

    /// <summary>
    /// Converts an absolute source file path to a repository-relative path.
    /// Strips the deterministic-build source-root prefix when present; otherwise prefers
    /// stripping the <c>GITHUB_WORKSPACE</c> prefix and falls back to locating the
    /// repository name within the path. Returns <see langword="null"/> when the path
    /// cannot be resolved to a repository-relative location.
    /// </summary>
    internal static string? ToRepoRelativePath(string? filePath, string? workspace, string? repo)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(repo))
        {
            return null;
        }

        var normalized = filePath!.Replace('\\', '/');

        // Deterministic builds collapse the workspace to "/_/", so check this first — in CI
        // the path will never start with the real GITHUB_WORKSPACE.
        var deterministicMatch = DeterministicRootRegex().Match(normalized);
        if (deterministicMatch.Success)
        {
            return normalized[deterministicMatch.Length..];
        }

        // Normalize the workspace here too so callers don't have to — keeps the prefix
        // match working regardless of which slash style the caller passes in.
        var normalizedWorkspace = workspace?.Replace('\\', '/').TrimEnd('/');

        // Prefer GITHUB_WORKSPACE for reliable path stripping; fall back to repo name matching.
        if (!string.IsNullOrEmpty(normalizedWorkspace) && normalized.StartsWith(normalizedWorkspace!, StringComparison.OrdinalIgnoreCase))
        {
            return normalized[normalizedWorkspace!.Length..].TrimStart('/');
        }

        var repoName = repo!.Split('/').LastOrDefault() ?? "";
        if (repoName.Length > 0)
        {
            var repoIndex = normalized.IndexOf($"/{repoName}/", StringComparison.OrdinalIgnoreCase);
            if (repoIndex >= 0)
            {
                return normalized[(repoIndex + repoName.Length + 2)..];
            }
        }

        return null;
    }
}
