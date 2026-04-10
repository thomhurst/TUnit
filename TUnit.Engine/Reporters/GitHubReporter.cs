using System.Collections.Concurrent;
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

    private readonly ConcurrentDictionary<string, List<TestNodeUpdateMessage>> _updates = [];

    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        var testNodeUpdateMessage = (TestNodeUpdateMessage) value;

        _updates.GetOrAdd(testNodeUpdateMessage.TestNode.Uid.Value, []).Add(testNodeUpdateMessage);

        return Task.CompletedTask;
    }

    public Type[] DataTypesConsumed { get; } = [typeof(TestNodeUpdateMessage)];

    public Task BeforeRunAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task AfterRunAsync(int exitCode, CancellationToken cancellation)
    {
        if (_updates.Count is 0)
        {
            return Task.CompletedTask;
        }

        var targetFramework = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<TargetFrameworkAttribute>()
                .SingleOrDefault()
                ?.FrameworkDisplayName
            ?? RuntimeInformation.FrameworkDescription;

        var last = new Dictionary<string, TestNodeUpdateMessage>(_updates.Count);
        foreach (var kvp in _updates)
        {
            if (kvp.Value.Count > 0)
            {
                last[kvp.Key] = kvp.Value[kvp.Value.Count - 1];
            }
        }

        var passedCount = last.Count(x =>
            x.Value.TestNode.Properties.AsEnumerable().Any(p => p is PassedTestNodeStateProperty));

        var failed = last.Where(x =>
            x.Value.TestNode.Properties.AsEnumerable()
                .Any(p => p is FailedTestNodeStateProperty or ErrorTestNodeStateProperty)).ToArray();

#pragma warning disable CS0618 // CancelledTestNodeStateProperty is obsolete
        var cancelled = last.Where(x =>
            x.Value.TestNode.Properties.AsEnumerable().Any(p => p is CancelledTestNodeStateProperty)).ToArray();
#pragma warning restore CS0618

        var timeout = last
            .Where(x => x.Value.TestNode.Properties.AsEnumerable().Any(p => p is TimeoutTestNodeStateProperty))
            .ToArray();

        var skipped = last
            .Where(x => x.Value.TestNode.Properties.AsEnumerable().Any(p => p is SkippedTestNodeStateProperty))
            .ToArray();

        var inProgress = last.Where(x =>
            x.Value.TestNode.Properties.AsEnumerable().Any(p => p is InProgressTestNodeStateProperty)).ToArray();

        var totalDuration = TimeSpan.Zero;
        foreach (var msg in last.Values)
        {
            var timing = msg.TestNode.Properties.AsEnumerable().OfType<TimingProperty>().FirstOrDefault();
            if (timing is not null)
            {
                totalDuration += timing.GlobalTiming.Duration;
            }
        }

        var hasFailures = failed.Length > 0 || timeout.Length > 0 || cancelled.Length > 0;
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
        stringBuilder.AppendLine($"**{totalCount} tests** completed in **{FormatDuration(totalDuration)}** \u2014 **{passRate:F1}%** passed");
        stringBuilder.AppendLine();

        var segments = new List<string> { $"\u2705 {passedCount} passed" };

        if (failed.Length > 0)
        {
            segments.Add($"\u274C {failed.Length} failed");
        }

        if (skipped.Length > 0)
        {
            segments.Add($"\u23ED\uFE0F {skipped.Length} skipped");
        }

        if (timeout.Length > 0)
        {
            segments.Add($"\u23F1\uFE0F {timeout.Length} timed out");
        }

        if (cancelled.Length > 0)
        {
            segments.Add($"\uD83D\uDEAB {cancelled.Length} cancelled");
        }

        if (inProgress.Length > 0)
        {
            segments.Add($"\u26A0\uFE0F {inProgress.Length} in progress");
        }

        stringBuilder.AppendLine(string.Join(" \u00B7 ", segments));

        // Detect flaky tests (passed after retry)
        var flakyTests = new List<(string Name, int Attempts, TimeSpan? Duration)>();
        foreach (var kvp in _updates)
        {
            var finalStateCount = 0;
            foreach (var update in kvp.Value)
            {
                var state = update.TestNode.Properties.SingleOrDefault<TestNodeStateProperty>();
                if (state is not null and not InProgressTestNodeStateProperty and not DiscoveredTestNodeStateProperty)
                {
                    finalStateCount++;
                }
            }

            if (finalStateCount > 1 && last.TryGetValue(kvp.Key, out var lastUpdate))
            {
                var passed = lastUpdate.TestNode.Properties.AsEnumerable().Any(p => p is PassedTestNodeStateProperty);
                if (passed)
                {
                    var testMethodIdentifier = lastUpdate.TestNode.Properties.AsEnumerable()
                        .OfType<TestMethodIdentifierProperty>().FirstOrDefault();
                    var className = testMethodIdentifier?.TypeName;
                    var displayName = lastUpdate.TestNode.DisplayName;
                    var name = string.IsNullOrEmpty(className) ? displayName : $"{className}.{displayName}";
                    var timing = lastUpdate.TestNode.Properties.AsEnumerable().OfType<TimingProperty>().FirstOrDefault();
                    flakyTests.Add((name, finalStateCount, timing?.GlobalTiming.Duration));
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

        if (skipped.Length > 0)
        {
            var skipGroups = skipped
                .Select(x => x.Value.TestNode.Properties.AsEnumerable()
                    .OfType<SkippedTestNodeStateProperty>().FirstOrDefault()?.Explanation ?? "No reason provided")
                .GroupBy(reason => reason)
                .OrderByDescending(g => g.Count());

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("<details>");
            stringBuilder.AppendLine($"<summary>\u23ed\ufe0f {skipped.Length} skipped {(skipped.Length == 1 ? "test" : "tests")}</summary>");
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

        if (failed.Length > 0)
        {
            var failureGroups = failed
                .Select(x =>
                {
                    var state = x.Value.TestNode.Properties.AsEnumerable().FirstOrDefault(p => p is TestNodeStateProperty);
                    var exceptionType = state switch
                    {
                        FailedTestNodeStateProperty f => f.Exception?.GetType().Name ?? "Unknown",
                        ErrorTestNodeStateProperty e => e.Exception?.GetType().Name ?? "Unknown",
                        _ => "Unknown"
                    };
                    var method = x.Value.TestNode.Properties.AsEnumerable()
                        .OfType<TestMethodIdentifierProperty>().FirstOrDefault();
                    return (ExceptionType: exceptionType, ClassName: method?.TypeName ?? "Unknown");
                })
                .GroupBy(x => x.ExceptionType)
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
            return WriteFile(stringBuilder.ToString());
        }

        // Separate failures from other non-passing tests
        var failureMessages = new List<(string Name, string? SourceLink, string Details, string Duration)>();
        var otherMessages = new List<(string Name, string Status, string Details, string Duration)>();

        foreach (var testNodeUpdateMessage in last.Values)
        {
            var passedProp = testNodeUpdateMessage.TestNode.Properties.OfType<PassedTestNodeStateProperty>().FirstOrDefault();
            if (passedProp != null) continue;

            var testMethodIdentifier = testNodeUpdateMessage.TestNode.Properties.AsEnumerable()
                .OfType<TestMethodIdentifierProperty>().FirstOrDefault();
            var className = testMethodIdentifier?.TypeName;
            var displayName = testNodeUpdateMessage.TestNode.DisplayName;
            var name = string.IsNullOrEmpty(className) ? displayName : $"{className}.{displayName}";
            var stateProperty = testNodeUpdateMessage.TestNode.Properties.AsEnumerable()
                .FirstOrDefault(p => p is TestNodeStateProperty);
            var timingProp = testNodeUpdateMessage.TestNode.Properties.AsEnumerable()
                .OfType<TimingProperty>().FirstOrDefault();
            var duration = FormatDuration(timingProp?.GlobalTiming.Duration);

            var isFailed = stateProperty is FailedTestNodeStateProperty or ErrorTestNodeStateProperty
                or TimeoutTestNodeStateProperty;

            if (isFailed)
            {
                var sourceLink = GetSourceLink(testNodeUpdateMessage.TestNode);
                var details = GetDetails(stateProperty, testNodeUpdateMessage.TestNode.Properties);
                failureMessages.Add((name, sourceLink, details, duration));
            }
            else
            {
                var status = GetStatus(stateProperty);
                var details = GetDetails(stateProperty, testNodeUpdateMessage.TestNode.Properties);
                otherMessages.Add((name, status, details, duration));
            }
        }

        // Show top failures inline
        const int maxInlineFailures = 5;
        if (failureMessages.Count > 0)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("#### Failures");
            stringBuilder.AppendLine();

            var inlineCount = Math.Min(failureMessages.Count, maxInlineFailures);
            for (int i = 0; i < inlineCount; i++)
            {
                var (name, sourceLink, details, duration) = failureMessages[i];
                var sourcePart = sourceLink is not null ? $" \u2014 {sourceLink}" : "";
                stringBuilder.AppendLine("<details>");
                stringBuilder.AppendLine($"<summary><code>{name}</code> ({duration}){sourcePart}</summary>");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(details);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("</details>");
            }

            if (failureMessages.Count > maxInlineFailures)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"*...and {failureMessages.Count - maxInlineFailures} more failures*");
            }
        }

        // Build the full details table for remaining items
        var remainingFailures = failureMessages.Count > maxInlineFailures
            ? failureMessages.Skip(maxInlineFailures).ToList()
            : new List<(string Name, string? SourceLink, string Details, string Duration)>();
        var hasRemainingDetails = remainingFailures.Count > 0 || otherMessages.Count > 0;

        if (hasRemainingDetails)
        {
            var detailsBuilder = new StringBuilder();
            detailsBuilder.AppendLine();
            detailsBuilder.AppendLine("""<table role="table" tabindex="0">""");
            detailsBuilder.AppendLine("<thead><tr><th>Test</th><th>Status</th><th>Details</th><th>Duration</th></tr></thead>");
            detailsBuilder.AppendLine("<tbody>");

            foreach (var (name, sourceLink, details, duration) in remainingFailures)
            {
                detailsBuilder.AppendLine("<tr>");
                detailsBuilder.AppendLine($"<td>{name}</td>");
                detailsBuilder.AppendLine("<td>Failed</td>");
                detailsBuilder.AppendLine($"<td>{details}</td>");
                detailsBuilder.AppendLine($"<td>{duration}</td>");
                detailsBuilder.AppendLine("</tr>");
            }

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
                var totalNonPassing = failureMessages.Count + otherMessages.Count;
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("<details>");
                stringBuilder.AppendLine($"<summary>All non-passing tests ({totalNonPassing} total)</summary>");
                stringBuilder.Append(detailsBuilder.ToString());
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("</details>");
            }
            else
            {
                stringBuilder.Append(detailsBuilder.ToString());
            }
        }

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
        var random = new Random();

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

    private string? GetError(IProperty? stateProperty)
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

    private static string? GetSourceLink(TestNode testNode)
    {
        var fileLocation = testNode.Properties.AsEnumerable()
            .OfType<TestFileLocationProperty>().FirstOrDefault();
        if (fileLocation is null) return null;

        var repo = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubRepository);
        var sha = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubSha);
        if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(sha)) return null;

        var filePath = fileLocation.FilePath.Replace('\\', '/');
        var repoName = repo.Split('/').LastOrDefault() ?? "";
        var repoIndex = filePath.IndexOf($"/{repoName}/", StringComparison.OrdinalIgnoreCase);
        if (repoIndex >= 0)
        {
            filePath = filePath[(repoIndex + repoName.Length + 2)..];
        }

        var line = fileLocation.LineSpan.Start.Line + 1; // 0-based to 1-based
        var fileName = Path.GetFileName(fileLocation.FilePath);
        return $"[{fileName}:{line}](https://github.com/{repo}/blob/{sha}/{filePath}#L{line})";
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
        var d => $"{d.Value.TotalHours:F0}h {d.Value.Minutes}m"
    };
}
