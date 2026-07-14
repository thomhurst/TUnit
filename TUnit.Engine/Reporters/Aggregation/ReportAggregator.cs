using System.IO;
using System.Security.Cryptography;
using System.Text;
using TUnit.Engine.Configuration;
using TUnit.Engine.Helpers;
using TUnit.Engine.Reporters.Html;

namespace TUnit.Engine.Reporters.Aggregation;

internal enum AggregationMode
{
    /// <summary>No aggregation — reporters behave per-process, as before.</summary>
    Disabled,

    /// <summary>
    /// Sidecars are persisted to the shared directory and every finishing process
    /// rewrites the merged HTML report and the marked GitHub step summary region.
    /// For sibling processes within a single step (e.g. <c>dotnet test</c> on a solution).
    /// </summary>
    Cooperative,

    /// <summary>
    /// Sidecars and the merged HTML are persisted, but no step summary is written at all.
    /// For pipelines that run test projects as separate steps: a final step runs
    /// <c>tunit-report merge --github-summary</c> to emit the single block.
    /// </summary>
    Defer,
}

/// <summary>
/// Cross-process report aggregation (issue #4522). Each TUnit process persists its
/// <see cref="ReportData"/> as a JSON sidecar into a directory shared by all sibling
/// processes of the run, then — under a cross-process file lock — re-renders the merged
/// outputs from every sidecar present so far. The last process to finish naturally leaves
/// the complete aggregate; no process ever needs to know whether it is the last one.
/// </summary>
internal sealed class ReportAggregator
{
    private const string SidecarSearchPattern = "*" + ReportDataJson.SidecarExtension;
    private const string LockFileName = ".tunit-aggregate.lock";
    private const string MergedReportFileName = "merged-report.html";

    // Lock contention is expected (N processes finishing together, each holding the lock
    // for a full merge), so wait far longer than the file-write retry defaults.
    private const int LockMaxAttempts = 60;
    private const int LockRetryDelayMs = 250;

    internal AggregationMode Mode { get; }
    internal string Directory { get; }
    internal string MergedReportPath => Path.Combine(Directory, MergedReportFileName);

    private ReportAggregator(AggregationMode mode, string directory)
    {
        Mode = mode;
        Directory = directory;
    }

    /// <summary>
    /// Reads TUNIT_AGGREGATE_REPORTS / TUNIT_AGGREGATE_DIR and resolves the shared
    /// directory. Aggregation is ON by default wherever a shared directory is resolvable
    /// (GitHub Actions, or an explicit TUNIT_AGGREGATE_DIR); plain local runs silently
    /// no-op. Returns <see langword="null"/> when aggregation is off or no shared
    /// directory can be derived.
    /// </summary>
    internal static ReportAggregator? TryCreateFromEnvironment(Func<string, string?> getEnv)
    {
        var raw = getEnv(EnvironmentConstants.AggregateReports)?.Trim().ToLowerInvariant();

        var mode = raw switch
        {
            "0" or "false" or "no" or "off" or "disabled" or "none" => AggregationMode.Disabled,
            "defer" => AggregationMode.Defer,
            // Unset, or any affirmative value (1/true/yes/cooperative): cooperative merge.
            _ => AggregationMode.Cooperative,
        };

        if (mode == AggregationMode.Disabled)
        {
            return null;
        }

        var directory = ResolveDirectory(getEnv);
        if (directory is null)
        {
            // Only warn when aggregation was explicitly requested — with the on-by-default
            // behaviour, every plain local run lands here and must stay silent.
            if (!string.IsNullOrEmpty(raw))
            {
                Console.WriteLine(
                    $"Warning: {EnvironmentConstants.AggregateReports} is set but no shared directory could be resolved. " +
                    $"Set {EnvironmentConstants.AggregateDirectory} to a directory shared by all test processes. Report aggregation is disabled for this run.");
            }
            return null;
        }

        return new ReportAggregator(mode, directory);
    }

    private static string? ResolveDirectory(Func<string, string?> getEnv)
    {
        var explicitDir = getEnv(EnvironmentConstants.AggregateDirectory);
        if (!string.IsNullOrWhiteSpace(explicitDir))
        {
            return Path.GetFullPath(explicitDir!);
        }

        // On GitHub Actions a job-scoped shared directory can be derived automatically:
        // RUNNER_TEMP is shared by every process in the job and cleaned between jobs.
        // Run id + attempt + job keep re-runs and sibling jobs on the same runner apart.
        if (getEnv(EnvironmentConstants.GitHubActions) is "true"
            && getEnv(EnvironmentConstants.RunnerTemp) is { Length: > 0 } runnerTemp)
        {
            var runId = getEnv(EnvironmentConstants.GitHubRunId) ?? "0";
            var attempt = getEnv(EnvironmentConstants.GitHubRunAttempt) ?? "1";
            var job = getEnv(EnvironmentConstants.GitHubJob) ?? "job";
            return Path.Combine(runnerTemp, "tunit-aggregate",
                PathValidator.SanitizeFileName($"run-{runId}-{attempt}-{job}"));
        }

        return null;
    }

    /// <summary>
    /// Persists this process's report data into the shared directory. The file name is
    /// stable per suite (assembly + report path hash), so a re-run within the same scope
    /// overwrites rather than duplicates. Written via temp-file + rename so concurrent
    /// readers never observe a torn sidecar.
    /// </summary>
    internal string WriteSidecar(ReportData data, string suiteSalt)
    {
        System.IO.Directory.CreateDirectory(Directory);

        var fileName = $"{PathValidator.SanitizeFileName(data.AssemblyName)}-{ShortHash(suiteSalt)}{ReportDataJson.SidecarExtension}";
        var path = Path.Combine(Directory, fileName);
        var tempPath = path + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp";

        File.WriteAllText(tempPath, ReportDataJson.Serialize(data), Encoding.UTF8);
#if NET
        File.Move(tempPath, path, overwrite: true);
#else
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        File.Move(tempPath, path);
#endif
        return path;
    }

    /// <summary>
    /// Reads every sidecar currently present in the shared directory. Unreadable or
    /// foreign files are skipped — a crashed sibling must not break the merge.
    /// </summary>
    internal List<ReportData> ReadAllSidecars()
    {
        var results = new List<ReportData>();
        if (!System.IO.Directory.Exists(Directory))
        {
            return results;
        }

        foreach (var file in System.IO.Directory.GetFiles(Directory, SidecarSearchPattern))
        {
            try
            {
                if (ReportDataJson.TryDeserialize(File.ReadAllText(file, Encoding.UTF8)) is { } data)
                {
                    results.Add(data);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Locked mid-write by a sibling (it re-merges after us anyway) or not
                // readable by this process — one bad file must not abort the whole merge.
            }
        }

        return results;
    }

    /// <summary>
    /// Acquires the cross-process aggregation lock. Every writer performs its whole
    /// read-merge-write cycle under this lock, so merges never interleave. Returns
    /// <see langword="null"/> when the lock cannot be acquired within the timeout;
    /// callers should then skip merging (a later sibling will produce a fresher merge).
    /// </summary>
    internal async Task<IDisposable?> AcquireLockAsync(CancellationToken cancellationToken)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var lockPath = Path.Combine(Directory, LockFileName);

        for (var attempt = 1; attempt <= LockMaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Contention (or a permissions hiccup) on the final attempt must still fall
                // through to the graceful "skip this merge" path, never escape as a throw.
                if (attempt == LockMaxAttempts)
                {
                    break;
                }

                await Task.Delay(LockRetryDelayMs + Random.Shared.Next(0, 100), cancellationToken);
            }
        }

        Console.WriteLine("Warning: Could not acquire the report aggregation lock; skipping merge for this process.");
        return null;
    }

    /// <summary>
    /// Regenerates the merged HTML report from all sidecars present. Caller holds the lock.
    /// </summary>
    internal void WriteMergedHtml(IReadOnlyList<ReportData> suites)
    {
        if (suites.Count == 0)
        {
            return;
        }

        var merged = ReportDataMerger.Merge(suites);
        var html = HtmlReportGenerator.GenerateHtml(merged);
        File.WriteAllText(MergedReportPath, html, Encoding.UTF8);
    }

    private static string ShortHash(string value)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        var sb = new StringBuilder(8);
        for (var i = 0; i < 4; i++)
        {
            sb.Append(hash[i].ToString("x2"));
        }
        return sb.ToString();
    }
}
