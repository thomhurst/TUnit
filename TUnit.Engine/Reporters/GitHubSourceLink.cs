namespace TUnit.Engine.Reporters;

/// <summary>
/// Shared helpers for turning a local test source file path into a GitHub
/// repository-relative path, used by both the GitHub and HTML reporters.
/// </summary>
internal static class GitHubSourceLink
{
    /// <summary>
    /// Converts an absolute source file path to a repository-relative path.
    /// Prefers stripping the <c>GITHUB_WORKSPACE</c> prefix; falls back to locating
    /// the repository name within the path. Returns <see langword="null"/> when the
    /// path cannot be resolved to a repository-relative location.
    /// </summary>
    internal static string? ToRepoRelativePath(string? filePath, string? workspace, string? repo)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(repo))
        {
            return null;
        }

        var normalized = filePath!.Replace('\\', '/');

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
