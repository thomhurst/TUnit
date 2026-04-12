using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
using TUnit.Engine.Configuration;
using TUnit.Engine.Constants;
using TUnit.Engine.Framework;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Reporters;

public enum GitHubReporterStyle
{
    Collapsible,
    Full
}

public class GitHubReporter(IExtension extension) : IDataConsumer, ITestHostApplicationLifetime, IFilterReceiver
{
    private const long MaxFileSizeInBytes = EngineDefaults.GitHubSummaryMaxFileSizeBytes;
    private string _outputSummaryFilePath = null!;
    private GitHubReporterStyle _reporterStyle = GitHubReporterStyle.Collapsible;
    private Stopwatch? _runStopwatch;

    public async Task<bool> IsEnabledAsync()
    {
        if (Environment.GetEnvironmentVariable(EnvironmentConstants.DisableGithubReporter) is not null ||
            Environment.GetEnvironmentVariable(EnvironmentConstants.DisableGithubReporterLegacy) is not null)
        {
            return false;
        }

        if (Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubActions) is null)
        {
            return false;
        }

        if (Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubStepSummary) is not { } fileName
            || !File.Exists(fileName))
        {
            return false;
        }

        // Validate and normalize the path to prevent path traversal attacks
        _outputSummaryFilePath = PathValidator.ValidateAndNormalizePath(fileName, "GITHUB_STEP_SUMMARY");

        // Determine reporter style from environment variable or default to collapsible
        var styleEnv = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubReporterStyle);
        if (!string.IsNullOrEmpty(styleEnv))
        {
            _reporterStyle = styleEnv!.ToLowerInvariant() switch
            {
                "full" => GitHubReporterStyle.Full,
                "collapsible" => GitHubReporterStyle.Collapsible,
                _ => GitHubReporterStyle.Collapsible
            };
        }

        return await extension.IsEnabledAsync();
    }

    public string Uid { get; } = $"{extension.Uid}GitHubReporter";

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    // Counts terminal state transitions per test UID (for flaky detection).
    private readonly ConcurrentDictionary<string, int> _terminalStateCounts = [];
    private readonly ConcurrentDictionary<string, TestNodeUpdateMessage> _latestUpdates = [];

    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        var testNodeUpdateMessage = (TestNodeUpdateMessage) value;

        var uid = testNodeUpdateMessage.TestNode.Uid.Value;

        var state = testNodeUpdateMessage.TestNode.Properties.SingleOrDefault<TestNodeStateProperty>();
        if (state is not null and not InProgressTestNodeStateProperty and not DiscoveredTestNodeStateProperty)
        {
            _terminalStateCounts.AddOrUpdate(uid, 1, static (_, count) => count + 1);
        }

        _latestUpdates[uid] = testNodeUpdateMessage;

        return Task.CompletedTask;
    }

    public Type[] DataTypesConsumed { get; } = [typeof(TestNodeUpdateMessage)];

    public Task BeforeRunAsync(CancellationToken cancellationToken)
    {
        _runStopwatch = Stopwatch.StartNew();
        return Task.CompletedTask;
    }

    public Task AfterRunAsync(int exitCode, CancellationToken cancellation)
    {
        if (_latestUpdates.IsEmpty)
        {
            return Task.CompletedTask;
        }

        var targetFramework = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<TargetFrameworkAttribute>()
                .SingleOrDefault()
                ?.FrameworkDisplayName
            ?? RuntimeInformation.FrameworkDescription;

        var last = new Dictionary<string, TestNodeUpdateMessage>(_latestUpdates.Count);
        foreach (var kvp in _latestUpdates)
        {
            last[kvp.Key] = kvp.Value;
        }

        var passedCount = 0;
        var failed = new List<KeyValuePair<string, TestNodeUpdateMessage>>();
        var cancelled = new List<KeyValuePair<string, TestNodeUpdateMessage>>();
        var timeout = new List<KeyValuePair<string, TestNodeUpdateMessage>>();
        var skipped = new List<KeyValuePair<string, TestNodeUpdateMessage>>();
        var inProgress = new List<KeyValuePair<string, TestNodeUpdateMessage>>();

        foreach (var kvp in last)
        {
            var state = kvp.Value.TestNode.Properties.OfType<TestNodeStateProperty>().FirstOrDefault();
            switch (state)
            {
                case PassedTestNodeStateProperty:
                    passedCount++;
                    break;
                case FailedTestNodeStateProperty or ErrorTestNodeStateProperty:
                    failed.Add(kvp);
                    break;
                case TimeoutTestNodeStateProperty:
                    timeout.Add(kvp);
                    break;
                case SkippedTestNodeStateProperty:
                    skipped.Add(kvp);
                    break;
                case InProgressTestNodeStateProperty:
                    inProgress.Add(kvp);
                    break;
#pragma warning disable CS0618
                case CancelledTestNodeStateProperty:
#pragma warning restore CS0618
                    cancelled.Add(kvp);
                    break;
            }
        }

        _runStopwatch?.Stop();
        var elapsed = _runStopwatch?.Elapsed;

        var hasFailures = failed.Count > 0 || timeout.Count > 0 || cancelled.Count > 0;
        var statusEmoji = hasFailures ? "\u274C" : "\u2705";

        var stringBuilder = new StringBuilder();

        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;

        if (!string.IsNullOrEmpty(ArtifactUrl))
        {
            stringBuilder.AppendLine($"### {statusEmoji} {assemblyName} ({targetFramework}) [(View Report)]({ArtifactUrl})");
        }
        else
        {
            stringBuilder.AppendLine($"### {statusEmoji} {assemblyName} ({targetFramework})");
        }

        if (!string.IsNullOrEmpty(Filter))
        {
            stringBuilder.AppendLine($"#### Filter: `{Filter}`");
        }

        var totalCount = last.Count;
        var passRate = totalCount > 0 ? (double)passedCount / totalCount * 100 : 0;

        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"**{totalCount} tests** completed in **{FormatDuration(elapsed)}** \u2014 **{passRate:F1}%** passed");
        stringBuilder.AppendLine();

        // Only show the segment breakdown when there's more than just "N passed"
        if (passedCount != totalCount)
        {
            var segments = new List<string> { $"\u2705 {passedCount} passed" };

            if (failed.Count > 0)
            {
                segments.Add($"\u274C {failed.Count} failed");
            }

            if (skipped.Count > 0)
            {
                segments.Add($"\u23ED\uFE0F {skipped.Count} skipped");
            }

            if (timeout.Count > 0)
            {
                segments.Add($"\u23F1\uFE0F {timeout.Count} timed out");
            }

            if (cancelled.Count > 0)
            {
                segments.Add($"\uD83D\uDEAB {cancelled.Count} cancelled");
            }

            if (inProgress.Count > 0)
            {
                segments.Add($"\u26A0\uFE0F {inProgress.Count} in progress");
            }

            stringBuilder.AppendLine(string.Join(" \u00B7 ", segments));
        }

        // Detect flaky tests (passed after retry)
        var flakyTests = new List<(string Name, int Attempts, TimeSpan? Duration)>();
        foreach (var kvp in _terminalStateCounts)
        {
            if (kvp.Value > 1 && last.TryGetValue(kvp.Key, out var lastUpdate))
            {
                var props = lastUpdate.TestNode.Properties.AsEnumerable();
                if (props.Any(p => p is PassedTestNodeStateProperty))
                {
                    var name = GetTestDisplayName(lastUpdate.TestNode);
                    var timing = props.OfType<TimingProperty>().FirstOrDefault();
                    flakyTests.Add((name, kvp.Value, timing?.GlobalTiming.Duration));
                }
            }
        }

        if (flakyTests.Count > 0)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"> **\u26a0\ufe0f {flakyTests.Count} flaky {(flakyTests.Count == 1 ? "test" : "tests")}** passed after retry:");
            foreach (var (name, attempts, duration) in flakyTests)
            {
                stringBuilder.AppendLine($"> - `{name}` \u2014 {attempts} attempts ({FormatDuration(duration)})");
            }
        }

        if (skipped.Count > 0)
        {
            var skipGroups = skipped
                .Select(x => x.Value.TestNode.Properties.AsEnumerable()
                    .OfType<SkippedTestNodeStateProperty>().FirstOrDefault()?.Explanation ?? "No reason provided")
                .GroupBy(reason => reason)
                .OrderByDescending(g => g.Count());

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("<details>");
            stringBuilder.AppendLine($"<summary>\u23ed\ufe0f {skipped.Count} skipped {(skipped.Count == 1 ? "test" : "tests")}</summary>");
            stringBuilder.AppendLine();
            foreach (var group in skipGroups)
            {
                stringBuilder.AppendLine($"- **{group.Count()}** \u2014 {group.Key}");
            }
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("</details>");
        }

        if (ShowArtifactUploadTip)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("> **Tip:** You can have HTML reports uploaded automatically as artifacts. [Learn more](https://tunit.dev/docs/guides/html-report#enabling-automatic-artifact-upload)");
        }

        // Cache env vars for source links (read once, not per test)
        var githubRepo = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubRepository);
        var githubSha = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubSha);
        var githubWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE")?.Replace('\\', '/');
        var githubServerUrl = Environment.GetEnvironmentVariable("GITHUB_SERVER_URL") ?? "https://github.com";

        // Separate failures from other non-passing tests (built once, used by both quick diagnosis and full rendering)
        var failureMessages = new List<FailureEntry>();
        var otherMessages = new List<(string Name, string Status, string Details, string Duration)>();

        foreach (var testNodeUpdateMessage in last.Values)
        {
            var props = testNodeUpdateMessage.TestNode.Properties.AsEnumerable();
            if (props.Any(p => p is PassedTestNodeStateProperty)) continue;

            var name = GetTestDisplayName(testNodeUpdateMessage.TestNode);
            var stateProperty = props.FirstOrDefault(p => p is TestNodeStateProperty);
            var timingProp = props.OfType<TimingProperty>().FirstOrDefault();
            var duration = FormatDuration(timingProp?.GlobalTiming.Duration);

            var isFailed = stateProperty is FailedTestNodeStateProperty or ErrorTestNodeStateProperty
                or TimeoutTestNodeStateProperty;

            if (isFailed)
            {
                var sourceLink = GetSourceLink(testNodeUpdateMessage.TestNode, githubRepo, githubSha, githubWorkspace, githubServerUrl);
                var exceptionType = GetExceptionTypeName(stateProperty);
                var commonError = GetError(stateProperty);
                var method = props.OfType<TestMethodIdentifierProperty>().FirstOrDefault();
                var className = method?.TypeName ?? "Unknown";
                failureMessages.Add(new FailureEntry(name, sourceLink, duration, exceptionType, commonError, className));
            }
            else
            {
                var status = GetStatus(stateProperty);
                var details = GetDetails(stateProperty, testNodeUpdateMessage.TestNode.Properties);
                otherMessages.Add((name, status, details, duration));
            }
        }

        if (failureMessages.Count > 0)
        {
            var failureGroups = failureMessages
                .GroupBy(f => f.ExceptionType)
                .OrderByDescending(g => g.Count())
                .Take(3);

            var diagParts = failureGroups.Select(g =>
            {
                var topClass = g.GroupBy(x => x.ClassName).OrderByDescending(c => c.Count()).First();
                return $"{g.Count()} \u00d7 `{g.Key}` in `{topClass.Key}`";
            });

            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"> **Quick diagnosis:** {string.Join(", ", diagParts)}");
        }

        if (passedCount == last.Count)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("---");
            return WriteFile(stringBuilder.ToString());
        }

        // Cap per group to keep the GitHub step summary within the 1 MB file-size limit
        const int maxTestsPerGroup = 50;
        if (failureMessages.Count > 0)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("#### Failures by Cause");
            stringBuilder.AppendLine();

            var grouped = failureMessages
                .GroupBy(f => f.ExceptionType)
                .OrderByDescending(g => g.Count());

            foreach (var group in grouped)
            {
                var entries = group.ToList();
                var count = entries.Count;
                var label = $"{group.Key} ({count} {(count == 1 ? "test" : "tests")})";

                if (_reporterStyle == GitHubReporterStyle.Collapsible)
                {
                    stringBuilder.AppendLine("<details>");
                    stringBuilder.AppendLine($"<summary>{label}</summary>");
                }
                else
                {
                    stringBuilder.AppendLine($"**{label}**");
                }

                stringBuilder.AppendLine();
                stringBuilder.AppendLine("| Test | Duration |");
                stringBuilder.AppendLine("| --- | --- |");

                var displayCount = Math.Min(count, maxTestsPerGroup);
                for (int i = 0; i < displayCount; i++)
                {
                    var entry = entries[i];
                    var sourcePart = entry.SourceLink is not null ? $" {entry.SourceLink}" : "";
                    stringBuilder.AppendLine($"| `{entry.Name}`{sourcePart} | {entry.Duration} |");
                }

                if (count > maxTestsPerGroup)
                {
                    stringBuilder.AppendLine($"| *...and {count - maxTestsPerGroup} more* | |");
                }

                var commonError = entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.CommonError))
                    .GroupBy(e => e.CommonError)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()
                    ?.Key;

                if (commonError is not null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("**Common error:**");
                    stringBuilder.AppendLine($"<pre>{System.Net.WebUtility.HtmlEncode(commonError)}</pre>");
                }

                if (_reporterStyle == GitHubReporterStyle.Collapsible)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("</details>");
                }

                stringBuilder.AppendLine();
            }
        }

        // Build the details table for other non-passing tests (cancelled, in-progress, etc.)
        if (otherMessages.Count > 0)
        {
            var detailsBuilder = new StringBuilder();
            detailsBuilder.AppendLine();
            detailsBuilder.AppendLine("""<table role="table" tabindex="0">""");
            detailsBuilder.AppendLine("<thead><tr><th>Test</th><th>Status</th><th>Details</th><th>Duration</th></tr></thead>");
            detailsBuilder.AppendLine("<tbody>");

            foreach (var (name, status, details, duration) in otherMessages)
            {
                detailsBuilder.AppendLine("<tr>");
                detailsBuilder.AppendLine($"<td>{name}</td>");
                detailsBuilder.AppendLine($"<td>{status}</td>");
                detailsBuilder.AppendLine($"<td>{details}</td>");
                detailsBuilder.AppendLine($"<td>{duration}</td>");
                detailsBuilder.AppendLine("</tr>");
            }

            detailsBuilder.AppendLine("</tbody></table>");

            if (_reporterStyle == GitHubReporterStyle.Collapsible)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("<details>");
                stringBuilder.AppendLine($"<summary>Other non-passing tests ({otherMessages.Count} total)</summary>");
                stringBuilder.Append(detailsBuilder.ToString());
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("</details>");
            }
            else
            {
                stringBuilder.Append(detailsBuilder.ToString());
            }
        }

        stringBuilder.AppendLine();
        stringBuilder.AppendLine("---");

        return WriteFile(stringBuilder.ToString());
    }

    private async Task WriteFile(string contents)
    {
        var fileInfo = new FileInfo(_outputSummaryFilePath);
        var currentFileSize = fileInfo.Exists ? fileInfo.Length : 0;
        long newContentSize = Encoding.UTF8.GetByteCount(contents);
        var newSize = currentFileSize + newContentSize;

        if (newSize > MaxFileSizeInBytes)
        {
            Console.WriteLine("Appending to the GitHub Step Summary would exceed the 1MB file size limit.");
            return;
        }

        const int maxAttempts = EngineDefaults.FileWriteMaxAttempts;
        var random = Random.Shared;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
#if NET
                await File.AppendAllTextAsync(_outputSummaryFilePath, contents, Encoding.UTF8);
#else
                File.AppendAllText(_outputSummaryFilePath, contents, Encoding.UTF8);
#endif
                return;
            }
            catch (IOException ex) when (attempt < maxAttempts && IsFileLocked(ex))
            {
                var baseDelay = EngineDefaults.BaseRetryDelayMs * Math.Pow(2, attempt - 1);
                var jitter = random.Next(0, EngineDefaults.MaxRetryJitterMs);
                var delay = (int)(baseDelay + jitter);
                
                Console.WriteLine($"GitHub Summary file is locked, retrying in {delay}ms (attempt {attempt}/{maxAttempts})");
                await Task.Delay(delay);
            }
        }
    }

    private static bool IsFileLocked(IOException exception)
    {
        // Check if the exception is due to the file being locked/in use
        // HResult 0x80070020 is ERROR_SHARING_VIOLATION on Windows
        // HResult 0x80070021 is ERROR_LOCK_VIOLATION on Windows
        var errorCode = exception.HResult & 0xFFFF;
        return errorCode == 0x20 || errorCode == 0x21 || 
               exception.Message.Contains("being used by another process") ||
               exception.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase);
    }

    private string GetDetails(IProperty? stateProperty, PropertyBag properties)
    {
#pragma warning disable CS0618 // CancelledTestNodeStateProperty is obsolete
        if (stateProperty is FailedTestNodeStateProperty
            or ErrorTestNodeStateProperty
            or TimeoutTestNodeStateProperty
            or CancelledTestNodeStateProperty)
#pragma warning restore CS0618
        {
            return $"<pre>{GetError(stateProperty)}</pre>";
        }

        if (stateProperty is SkippedTestNodeStateProperty skippedTestNodeStateProperty)
        {
            return skippedTestNodeStateProperty.Explanation ?? "Skipped (No reason provided)";
        }

        if (stateProperty is InProgressTestNodeStateProperty)
        {
            var timingProperty = properties.AsEnumerable().OfType<TimingProperty>().FirstOrDefault();

            return $"Duration: {timingProperty?.GlobalTiming.Duration}";
        }

        return "Unknown Test State";
    }

    private static string? GetError(IProperty? stateProperty)
    {
        return stateProperty switch
        {
            FailedTestNodeStateProperty failedTestNodeStateProperty =>
                GetTruncatedExceptionMessage(failedTestNodeStateProperty.Exception) ?? "Test failed",
            ErrorTestNodeStateProperty errorTestNodeStateProperty =>
                GetTruncatedExceptionMessage(errorTestNodeStateProperty.Exception) ?? "Test failed",
            TimeoutTestNodeStateProperty timeoutTestNodeStateProperty => timeoutTestNodeStateProperty.Explanation,
#pragma warning disable CS0618 // CancelledTestNodeStateProperty is obsolete
            CancelledTestNodeStateProperty => "Test was cancelled",
#pragma warning restore CS0618
            _ => null
        };
    }

    private static string? GetTruncatedExceptionMessage(Exception? exception)
    {
        if (exception is null)
        {
            return null;
        }

        var message = exception.Message;

        var firstStackTraceLine = exception.StackTrace?.Split('\n').FirstOrDefault()?.Trim();

        if (string.IsNullOrWhiteSpace(firstStackTraceLine))
        {
            return message;
        }

        return $"{message}\n{firstStackTraceLine}";
    }

    private static string GetStatus(IProperty? stateProperty)
    {
        return stateProperty switch
        {
#pragma warning disable CS0618 // CancelledTestNodeStateProperty is obsolete
            CancelledTestNodeStateProperty => "Cancelled",
#pragma warning restore CS0618
            ErrorTestNodeStateProperty => "Failed",
            FailedTestNodeStateProperty => "Failed",
            InProgressTestNodeStateProperty => "In Progress (never finished)",
            PassedTestNodeStateProperty => "Passed",
            SkippedTestNodeStateProperty => "Skipped",
            TimeoutTestNodeStateProperty => "Timed Out",
            _ => "Unknown"
        };
    }

    private static string GetTestDisplayName(TestNode testNode)
    {
        var testMethodIdentifier = testNode.Properties.AsEnumerable()
            .OfType<TestMethodIdentifierProperty>().FirstOrDefault();
        var className = testMethodIdentifier?.TypeName;
        var displayName = testNode.DisplayName;
        return string.IsNullOrEmpty(className) ? displayName : $"{className}.{displayName}";
    }

    private static string? GetSourceLink(TestNode testNode, string? repo, string? sha, string? workspace, string serverUrl)
    {
        var fileLocation = testNode.Properties.AsEnumerable()
            .OfType<TestFileLocationProperty>().FirstOrDefault();
        if (fileLocation is null) return null;

        if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(sha)) return null;

        var filePath = fileLocation.FilePath.Replace('\\', '/');

        // Prefer GITHUB_WORKSPACE for reliable path stripping; fall back to repo name matching
        if (!string.IsNullOrEmpty(workspace) && filePath.StartsWith(workspace!, StringComparison.OrdinalIgnoreCase))
        {
            filePath = filePath[workspace!.Length..].TrimStart('/');
        }
        else
        {
            var repoName = repo!.Split('/').LastOrDefault() ?? "";
            var repoIndex = filePath.IndexOf($"/{repoName}/", StringComparison.OrdinalIgnoreCase);
            if (repoIndex >= 0)
            {
                filePath = filePath[(repoIndex + repoName.Length + 2)..];
            }
        }

        var line = fileLocation.LineSpan.Start.Line + 1; // 0-based to 1-based
        var fileName = Path.GetFileName(fileLocation.FilePath);
        return $"[{fileName}:{line}]({serverUrl.TrimEnd('/')}/{repo}/blob/{sha}/{filePath}#L{line})";
    }

    public string? Filter { get; set; }

    // Set by HtmlReporter during OnTestSessionFinishingAsync, which MTP invokes before AfterRunAsync.
    internal string? ArtifactUrl { get; set; }
    internal bool ShowArtifactUploadTip { get; set; }

    internal void SetReporterStyle(GitHubReporterStyle style)
    {
        _reporterStyle = style;
    }

    private static string FormatDuration(TimeSpan? duration) => duration switch
    {
        null => "-",
        { TotalMilliseconds: < 1 } => "< 1ms",
        { TotalSeconds: < 1 } d => $"{d.TotalMilliseconds:F0}ms",
        { TotalMinutes: < 1 } d => $"{d.TotalSeconds:F1}s",
        { TotalHours: < 1 } d => $"{d.Minutes}m {d.Seconds}s",
        var d => $"{(int)d.Value.TotalHours}h {d.Value.Minutes}m"
    };

    private static string GetExceptionTypeName(IProperty? stateProperty) => stateProperty switch
    {
        FailedTestNodeStateProperty f => f.Exception?.GetType().Name ?? "Unknown",
        ErrorTestNodeStateProperty e => e.Exception?.GetType().Name ?? "Unknown",
        TimeoutTestNodeStateProperty => "Timeout",
        _ => "Unknown"
    };

    private record FailureEntry(
        string Name, string? SourceLink, string Duration,
        string ExceptionType, string? CommonError, string ClassName);
}
