using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.Configuration;
using TUnit.Engine.Constants;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Framework;
using TUnit.Engine.Helpers;
using TUnit.Engine.Reporters;

#pragma warning disable TPEXP

namespace TUnit.Engine.Reporters.Html;

internal sealed class HtmlReporter(IExtension extension) : IDataConsumer, IDataProducer, ITestHostApplicationLifetime, ITestSessionLifetimeHandler, IFilterReceiver, IDisposable
{
    // System.Text.Json's Utf8JsonWriter limits a single string token to int.MaxValue / 6 characters.
    // Truncate large outputs early so report generation never fails for test suites with excessive logging.
    internal const int MaxOutputLength = 1 * 1024 * 1024; // 1 MB

    private string? _outputPath;
    private IMessageBus? _messageBus;
    private string _resultsDirectory = "TestResults";
    private readonly ConcurrentDictionary<string, TestNodeUpdateMessage> _updates = [];
    private GitHubReporter? _githubReporter;

#if NET
    private ActivityCollector? _activityCollector;
#endif

    public async Task<bool> IsEnabledAsync()
    {
        var disableValue = Environment.GetEnvironmentVariable(EnvironmentConstants.DisableHtmlReporter);
        if (disableValue is not null &&
            (disableValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
             disableValue.Equals("1", StringComparison.Ordinal) ||
             disableValue.Equals("yes", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return await extension.IsEnabledAsync();
    }

    public string Uid { get; } = $"{extension.Uid}HtmlReporter";

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        var testNodeUpdateMessage = (TestNodeUpdateMessage)value;
        // Keep only the update we'll report per test: a final-state update always wins over a
        // non-final one, otherwise the latest wins. The engine emits a single final update per
        // test, so storing the whole stream (the old ConcurrentQueue) just wasted memory.
        _updates.AddOrUpdate(
            testNodeUpdateMessage.TestNode.Uid.Value,
            testNodeUpdateMessage,
            (_, existing) => PreferForReport(existing, testNodeUpdateMessage));
        return Task.CompletedTask;
    }

    // Selects which update to keep for the report when more than one arrives for a test: a
    // final-state update always wins over a non-final one; otherwise the later (incoming) one
    // wins. Mirrors the previous "last final, else last overall" walk over the per-test queue.
    private static TestNodeUpdateMessage PreferForReport(TestNodeUpdateMessage existing, TestNodeUpdateMessage incoming)
        => IsFinalState(incoming) || !IsFinalState(existing) ? incoming : existing;

    private static bool IsFinalState(TestNodeUpdateMessage update)
    {
        var state = update.TestNode.Properties.SingleOrDefault<TestNodeStateProperty>();
        return state is not null and not InProgressTestNodeStateProperty and not DiscoveredTestNodeStateProperty;
    }

    public Type[] DataTypesConsumed { get; } = [typeof(TestNodeUpdateMessage)];

    public Type[] DataTypesProduced { get; } = [typeof(SessionFileArtifact)];

    public Task BeforeRunAsync(CancellationToken cancellationToken)
    {
#if NET
        _activityCollector = new ActivityCollector();
        _activityCollector.Start();
#endif
        return Task.CompletedTask;
    }

    public Task AfterRunAsync(int exitCode, CancellationToken cancellation)
        => Task.CompletedTask; // All work happens in OnTestSessionFinishingAsync.

    public Task OnTestSessionStartingAsync(ITestSessionContext testSessionContext)
        => Task.CompletedTask;

    public async Task OnTestSessionFinishingAsync(ITestSessionContext testSessionContext)
    {
        try
        {
#if NET
            _activityCollector?.Stop();
#endif

            if (_updates.Count == 0)
            {
#if NET
                TraceRegistry.Clear();
#endif
                return;
            }

            ReportData reportData;
            try
            {
                reportData = BuildReportData();
            }
            finally
            {
#if NET
                TraceRegistry.Clear();
#endif
            }

            var html = HtmlReportGenerator.GenerateHtml(reportData);

            if (string.IsNullOrEmpty(html))
            {
                return;
            }

            if (string.IsNullOrEmpty(_outputPath))
            {
                _outputPath = GetDefaultOutputPath();
            }

            var outputPath = _outputPath!;
            // WriteFileAsync returns false if all retry attempts are exhausted (locked file, bad path, etc.).
            // Artifact publishing is gated on a successful write — no file means no artifact.
            var written = await WriteFileAsync(outputPath, html, testSessionContext.CancellationToken);

            if (written)
            {
                await PublishArtifactAsync(outputPath, testSessionContext.SessionUid, testSessionContext.CancellationToken);
            }

            // GitHub Actions integration (artifact upload + step summary)
            await TryGitHubIntegrationAsync(outputPath, testSessionContext.CancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: HTML report generation failed: {ex.Message}");
        }
    }

    internal async Task PublishArtifactAsync(string outputPath, SessionUid sessionUid, CancellationToken cancellationToken)
    {
        if (_messageBus is null)
        {
            return;
        }

        // SessionFileArtifact is consumed by MTP itself (not user-defined consumers),
        // so no AddDataProducer registration is required — same pattern as TUnitMessageBus.
        await _messageBus.PublishAsync(this, new SessionFileArtifact(
            sessionUid,
            new FileInfo(outputPath),
            "HTML Test Report",
            "TUnit HTML test results report"));
    }

    public void Dispose()
    {
#if NET
        _activityCollector?.Dispose();
#endif
    }

    public string? Filter { get; set; }

    internal void SetOutputPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Output path cannot be null or empty", nameof(path));
        }

        _outputPath = path;
    }

    // Called by the AddTestSessionLifetimeHandler factory at startup, before any session events fire,
    // so _messageBus is guaranteed to be set before OnTestSessionFinishingAsync is invoked.
    internal void SetMessageBus(IMessageBus? messageBus)
    {
        _messageBus = messageBus;
    }

    internal void SetGitHubReporter(GitHubReporter githubReporter)
    {
        _githubReporter = githubReporter;
    }

    // Called by the AddTestSessionLifetimeHandler factory at startup, before any session events fire,
    // so _resultsDirectory is guaranteed to be set before OnTestSessionFinishingAsync is invoked.
    internal void SetResultsDirectory(string path)
    {
        _resultsDirectory = path;
    }

    internal ReportData BuildReportData()
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "TestResults";
        var tunitVersion = typeof(HtmlReporter).Assembly.GetName().Version?.ToString() ?? "unknown";

        // Get the last update with a final state for each test
        // Each test's update was already reduced to the one we report (final-state preferred) in
        // ConsumeAsync, so _updates maps directly to the nodes to render.
        var lastUpdates = _updates;

        var summary = new ReportSummary();
        var groupsByClass = new Dictionary<string, List<ReportTestResult>>();
        var groupNamespaces = new Dictionary<string, string>();
        double overallStartMs = double.MaxValue;
        double overallEndMs = double.MinValue;

        // Build span lookup to correlate traces with test results
#if NET
        var spanLookup = _activityCollector?.GetTestSpanLookup();
#else
        var spanLookup = (Dictionary<string, (string TraceId, string SpanId)>?)null;
#endif

        // Resolve source-control context once; reused for per-test source links and report metadata below.
        var ci = SourceControlContext.Detect(Environment.GetEnvironmentVariable);

        foreach (var kvp in lastUpdates)
        {
            var testNode = kvp.Value.TestNode;

            // Correlate trace/span IDs from collected activities
            string? traceId = null, spanId = null;
            if (spanLookup?.TryGetValue(kvp.Key, out var spanInfo) == true)
            {
                traceId = spanInfo.TraceId;
                spanId = spanInfo.SpanId;
            }

            // Build the per-attempt history for the flaky/retry UI. The engine emits only one
            // update per test (the final result), so we cannot reconstruct attempts from the
            // update stream. Instead, failed attempts that triggered a retry are captured during
            // execution and carried here on the final node via TUnitRetryAttemptsProperty; the
            // final attempt is the node's own state. We stitch the two together in order.
            ReportAttempt[]? attempts = null;
            var retryAttempt = 0;
            var priorAttempts = testNode.Properties.AsEnumerable()
                .OfType<TUnitRetryAttemptsProperty>()
                .FirstOrDefault();
            if (priorAttempts is { Attempts.Count: > 0 })
            {
                var finalState = testNode.Properties.SingleOrDefault<TestNodeStateProperty>();
                var (finalStatus, finalException, _) = ExtractStatus(finalState);
                var finalDuration = testNode.Properties.AsEnumerable()
                    .OfType<TimingProperty>()
                    .FirstOrDefault()?.GlobalTiming.Duration.TotalMilliseconds ?? 0;

                var attemptList = new List<ReportAttempt>(priorAttempts.Attempts.Count + 1);
                foreach (var prior in priorAttempts.Attempts)
                {
                    attemptList.Add(new ReportAttempt
                    {
                        Status = StatusFromState(prior.State),
                        DurationMs = prior.Duration.TotalMilliseconds,
                        ExceptionType = prior.ExceptionType,
                        ExceptionMessage = prior.ExceptionMessage,
                        StackTrace = prior.ExceptionStackTrace,
                    });
                }

                attemptList.Add(new ReportAttempt
                {
                    Status = finalStatus,
                    DurationMs = finalDuration,
                    ExceptionType = finalException?.Type,
                    ExceptionMessage = finalException?.Message,
                    StackTrace = finalException?.StackTrace,
                });

                retryAttempt = attemptList.Count - 1;
                attempts = attemptList.ToArray();
            }

#if NET
            var additionalTraceIds = FilterAdditionalTraceIds(TraceRegistry.GetTraceIds(kvp.Key), traceId);
            string[]? additionalTraceIdsForResult = additionalTraceIds.Length > 0 ? additionalTraceIds : null;
#else
            string[]? additionalTraceIdsForResult = null;
#endif

            var testResult = ExtractTestResult(kvp.Key, testNode, traceId, spanId, retryAttempt, additionalTraceIdsForResult, attempts, ci.RepositorySlug, ci.Workspace);

            AccumulateStatus(summary, testResult);

            // Group by class name
            var className = testResult.ClassName;
            if (!groupsByClass.TryGetValue(className, out var list))
            {
                list = [];
                groupsByClass[className] = list;
            }

            list.Add(testResult);

            // Track namespace
            var testMethodIdentifier = testNode.Properties.AsEnumerable()
                .OfType<TestMethodIdentifierProperty>()
                .FirstOrDefault();
            if (testMethodIdentifier != null && !groupNamespaces.ContainsKey(className))
            {
                groupNamespaces[className] = testMethodIdentifier.Namespace;
            }

            // Track overall timing
            var timingProperty = testNode.Properties.AsEnumerable()
                .OfType<TimingProperty>()
                .FirstOrDefault();
            if (timingProperty?.GlobalTiming is { } globalTiming)
            {
                var startMs = globalTiming.StartTime.ToUnixTimeMilliseconds();
                var endMs = (globalTiming.StartTime + globalTiming.Duration).ToUnixTimeMilliseconds();
                if (startMs < overallStartMs)
                {
                    overallStartMs = startMs;
                }

                if (endMs > overallEndMs)
                {
                    overallEndMs = endMs;
                }
            }
        }

        var totalDurationMs = overallStartMs < double.MaxValue ? overallEndMs - overallStartMs : 0;

        // Build groups
        var groups = new ReportTestGroup[groupsByClass.Count];
        var i = 0;
        foreach (var kvp in groupsByClass)
        {
            var groupSummary = new ReportSummary();
            foreach (var test in kvp.Value)
            {
                AccumulateStatus(groupSummary, test);
            }

            groups[i++] = new ReportTestGroup
            {
                ClassName = kvp.Key,
                Namespace = groupNamespaces.GetValueOrDefault(kvp.Key, ""),
                Summary = groupSummary,
                Tests = OrderTestsForDisplay(kvp.Value)
            };
        }

        // Collect spans
        SpanData[]? spans = null;
#if NET
        if (_activityCollector != null)
        {
            spans = _activityCollector.GetAllSpans();

            // Use the session span duration as the header duration when available,
            // since it captures the full wall-clock time including initialization.
            // The test-timing-based duration only covers test execution.
            var sessionSpan = spans?.FirstOrDefault(s => s.SpanType == TUnitActivitySource.SpanTestSession);
            if (sessionSpan != null)
            {
                totalDurationMs = sessionSpan.DurationMs;
            }
        }
#endif

        return new ReportData
        {
            AssemblyName = assemblyName,
            MachineName = Environment.MachineName,
            Timestamp = DateTimeOffset.UtcNow.ToString("dd MMM yyyy, HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
            TUnitVersion = tunitVersion,
            OperatingSystem = RuntimeInformation.OSDescription,
            RuntimeVersion = RuntimeInformation.FrameworkDescription,
            Filter = Filter,
            TotalDurationMs = totalDurationMs,
            Summary = summary,
            Groups = groups,
            Spans = spans,
            CommitSha = ci.CommitSha,
            Branch = ci.Branch,
            PullRequestNumber = ci.PullRequestNumber,
            RepositorySlug = ci.RepositorySlug,
            SourceLinks = ci.Links,
        };
    }

    private static void AccumulateStatus(ReportSummary summary, ReportTestResult testResult)
    {
        summary.Total++;
        switch (testResult.Status)
        {
            case "passed" when testResult.RetryAttempt > 0:
                summary.Passed++;
                summary.Flaky++;
                break;
            case "passed":
                summary.Passed++;
                break;
            case "failed" or "error":
                summary.Failed++;
                break;
            case "skipped":
                summary.Skipped++;
                break;
            case "timedOut":
                summary.TimedOut++;
                break;
            case "cancelled":
                summary.Cancelled++;
                break;
        }
    }

#if NET
    // The engine auto-registers the test's own traceId in TraceRegistry for OTLP correlation,
    // so it shows up in GetTraceIds alongside any user-added traces. Strip it here so the
    // primary trace (rendered as "Trace Timeline") isn't also rendered as a "Linked Trace".
    internal static string[] FilterAdditionalTraceIds(string[] allTraceIds, string? primaryTraceId)
    {
        if (allTraceIds.Length == 0 || primaryTraceId is null)
        {
            return allTraceIds;
        }

        var filtered = new List<string>(allTraceIds.Length);
        foreach (var tid in allTraceIds)
        {
            if (!string.Equals(tid, primaryTraceId, StringComparison.OrdinalIgnoreCase))
            {
                filtered.Add(tid);
            }
        }

        return filtered.Count == allTraceIds.Length ? allTraceIds : filtered.ToArray();
    }
#endif

    internal static ReportTestResult[] OrderTestsForDisplay(IEnumerable<ReportTestResult> tests)
    {
        // Parse to DateTimeOffset so the sort works regardless of how the caller formatted
        // StartTime — production writes UTC ISO-8601, but tests construct ReportTestResult
        // directly via InternalsVisibleTo and could pass non-UTC offsets.
        return tests
            .OrderBy(static test => ParseStartTimeForSort(test.StartTime))
            .ThenBy(static test => test.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    private static DateTimeOffset ParseStartTimeForSort(string? raw)
    {
        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : DateTimeOffset.MaxValue;
    }

    internal static ReportTestResult ExtractTestResult(string testId, TestNode testNode, string? traceId, string? spanId, int retryAttempt, string[]? additionalTraceIds, ReportAttempt[]? attempts = null, string? ciRepo = null, string? ciWorkspace = null)
    {
        IProperty? stateProperty = null;
        TestMethodIdentifierProperty? testMethodIdentifier = null;
        TimingProperty? timingProperty = null;
        TestFileLocationProperty? fileLocation = null;
        string? stdOut = null;
        string? stdErr = null;
        List<string>? categories = null;
        List<ReportKeyValue>? customProperties = null;

        foreach (var prop in testNode.Properties.AsEnumerable())
        {
            switch (prop)
            {
                case TestNodeStateProperty when stateProperty is null:
                    stateProperty = prop;
                    break;
                case TestMethodIdentifierProperty m:
                    testMethodIdentifier = m;
                    break;
                case TimingProperty t:
                    timingProperty = t;
                    break;
                case TestFileLocationProperty f:
                    fileLocation = f;
                    break;
                case StandardOutputProperty o:
                    stdOut = TruncateOutput(o.StandardOutput);
                    break;
                case StandardErrorProperty e:
                    stdErr = TruncateOutput(e.StandardError);
                    break;
                case TestMetadataProperty meta:
                    // MTP convention (matches Microsoft.Testing.Extensions.VSTestBridge): categories are
                    // emitted as TestMetadataProperty(category, "") — name in Key, empty Value. Traits/
                    // custom properties use (key, value) with a non-empty Value.
                    if (string.IsNullOrEmpty(meta.Value))
                    {
                        categories ??= [];
                        categories.Add(meta.Key);
                    }
                    else
                    {
                        customProperties ??= [];
                        customProperties.Add(new ReportKeyValue { Key = meta.Key, Value = meta.Value });
                    }
                    break;
            }
        }

        var categoriesArray = categories?.ToArray();
        var customPropertiesArray = customProperties?.ToArray();

        var className = testMethodIdentifier?.TypeName ?? "UnknownClass";
        var methodName = testMethodIdentifier?.MethodName ?? testNode.DisplayName;

        var (status, exception, skipReason) = ExtractStatus(stateProperty);

        var durationMs = timingProperty?.GlobalTiming.Duration.TotalMilliseconds ?? 0;
        var startTime = timingProperty?.GlobalTiming.StartTime;
        var endTime = startTime.HasValue ? startTime.Value + timingProperty!.GlobalTiming.Duration : (DateTimeOffset?)null;

        return new ReportTestResult
        {
            Id = testId,
            DisplayName = testNode.DisplayName,
            MethodName = methodName,
            ClassName = className,
            Status = status,
            DurationMs = durationMs,
            StartTime = startTime?.ToUniversalTime().ToString("o"),
            EndTime = endTime?.ToUniversalTime().ToString("o"),
            Exception = exception,
            Output = stdOut,
            ErrorOutput = stdErr,
            Categories = categoriesArray is { Length: > 0 } ? categoriesArray : null,
            CustomProperties = customPropertiesArray is { Length: > 0 } ? customPropertiesArray : null,
            FilePath = fileLocation?.FilePath,
            LineNumber = fileLocation?.LineSpan.Start.Line,
            // Line numbers are already 1-based here. Emit an end line only when the span covers
            // more than the declaration line (source-gen) — reflection has none, so leave it null.
            EndLineNumber = fileLocation?.LineSpan.End.Line is { } endLine && endLine > fileLocation.LineSpan.Start.Line ? endLine : null,
            SourceRelativePath = SourcePathResolver.ToRepoRelativePath(fileLocation?.FilePath, ciWorkspace, ciRepo),
            SkipReason = skipReason,
            RetryAttempt = retryAttempt,
            Attempts = attempts,
            TraceId = traceId,
            SpanId = spanId,
            AdditionalTraceIds = additionalTraceIds
        };
    }

    internal static string? TruncateOutput(string? value)
    {
        if (value is null || value.Length <= MaxOutputLength)
        {
            return value;
        }

        // Back off one char if we would split a surrogate pair, which would produce invalid UTF-16.
        var cutAt = MaxOutputLength;
        if (char.IsHighSurrogate(value[cutAt - 1]))
        {
            cutAt--;
        }

        return new System.Text.StringBuilder(cutAt + 64)
            .Append(value, 0, cutAt)
            .Append($"\n[... output truncated \u2014 {value.Length:N0} total characters]")
            .ToString();
    }

    private static (string Status, ReportExceptionData? Exception, string? SkipReason) ExtractStatus(IProperty? stateProperty)
    {
        return stateProperty switch
        {
            PassedTestNodeStateProperty => ("passed", null, null),
            FailedTestNodeStateProperty failed => ("failed", MapException(failed.Exception), null),
            ErrorTestNodeStateProperty error => ("error", MapException(error.Exception), null),
            TimeoutTestNodeStateProperty timeout => ("timedOut", MapException(timeout.Exception), null),
            SkippedTestNodeStateProperty skipped => ("skipped", null, skipped.Explanation),
#pragma warning disable CS0618
            CancelledTestNodeStateProperty => ("cancelled", null, null),
#pragma warning restore CS0618
            InProgressTestNodeStateProperty => ("inProgress", null, null),
            _ => ("unknown", null, null)
        };
    }

    // Maps a captured retry attempt's TestState to the same status vocabulary ExtractStatus
    // produces for the final node, so prior and final attempts render consistently. A retried
    // attempt is always a failure of some kind; Failed maps to "failed" (HtmlReportGenerator's
    // MapStatus collapses "failed"/"error"/"timedOut" to "fail" for the UI).
    private static string StatusFromState(TestState state) => state switch
    {
        TestState.Passed => "passed",
        TestState.Failed => "failed",
        TestState.Timeout => "timedOut",
        TestState.Skipped => "skipped",
        TestState.Cancelled => "cancelled",
        _ => "error",
    };

    private static ReportExceptionData? MapException(Exception? ex)
    {
        if (ex is null)
        {
            return null;
        }

        ex = TUnitFailedException.Unwrap(ex);

        return new ReportExceptionData
        {
            Type = ex.GetType().FullName ?? ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            InnerException = MapException(ex.InnerException)
        };
    }

    private string GetDefaultOutputPath()
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "TestResults";
        var sanitizedName = PathValidator.SanitizeFileName(assemblyName);
        var os = GetShortOsName();
        var tfm = GetShortFrameworkName();
        return Path.GetFullPath(Path.Combine(_resultsDirectory, $"{sanitizedName}-{os}-{tfm}-report.html"));
    }

    private static string GetShortOsName()
    {
#if NET
        if (OperatingSystem.IsWindows()) return "windows";
        if (OperatingSystem.IsLinux()) return "linux";
        if (OperatingSystem.IsMacOS()) return "macos";
#else
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macos";
#endif
        return "unknown";
    }

    private static string GetShortFrameworkName()
    {
        // RuntimeInformation.FrameworkDescription returns e.g. ".NET 10.0.0" or ".NET Framework 4.8.0"
        var desc = RuntimeInformation.FrameworkDescription;
        if (desc.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase))
        {
            var version = desc.Substring(".NET Framework ".Length).Trim();
            var dotIndex = version.IndexOf('.');
            if (dotIndex > 0)
            {
                var secondDot = version.IndexOf('.', dotIndex + 1);
                if (secondDot > 0) version = version.Substring(0, secondDot);
            }
            return $"net{version.Replace(".", "")}";
        }

        if (desc.StartsWith(".NET ", StringComparison.OrdinalIgnoreCase))
        {
            var version = desc.Substring(".NET ".Length).Trim();
            var dotIndex = version.IndexOf('.');
            if (dotIndex > 0)
            {
                var secondDot = version.IndexOf('.', dotIndex + 1);
                if (secondDot > 0) version = version.Substring(0, secondDot);
            }
            return $"net{version}";
        }

        return "unknown";
    }

    private static async Task<bool> WriteFileAsync(string path, string content, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        const int maxAttempts = EngineDefaults.FileWriteMaxAttempts;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
#if NET
                await File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken);
#else
                File.WriteAllText(path, content, Encoding.UTF8);
#endif
                Console.WriteLine($"HTML test report written to: {path}");
                return true;
            }
            catch (IOException ex) when (attempt < maxAttempts && IsFileLocked(ex))
            {
                var baseDelay = EngineDefaults.BaseRetryDelayMs * Math.Pow(2, attempt - 1);
                var jitter = Random.Shared.Next(0, EngineDefaults.MaxRetryJitterMs);
                var delay = (int)(baseDelay + jitter);

                Console.WriteLine($"HTML report file is locked, retrying in {delay}ms (attempt {attempt}/{maxAttempts})");
                await Task.Delay(delay, cancellationToken);
            }
        }

        Console.WriteLine($"Failed to write HTML test report to: {path} after {maxAttempts} attempts");
        return false;
    }

    private static bool IsFileLocked(IOException exception)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var errorCode = exception.HResult & 0xFFFF;
            return errorCode is 0x20 or 0x21; // ERROR_SHARING_VIOLATION / ERROR_LOCK_VIOLATION
        }

        // On POSIX, concurrent writers are less common; fallback to message heuristic
        return exception.Message.Contains("being used by another process") ||
               exception.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase);
    }

    private async Task TryGitHubIntegrationAsync(string filePath, CancellationToken cancellationToken)
    {
        if (Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubActions) is not "true")
        {
            return;
        }

        var repo = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubRepository);
        var runId = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubRunId);

        // Try in-process artifact upload if the runtime token is available
        string? artifactId = null;
        var runtimeToken = Environment.GetEnvironmentVariable(EnvironmentConstants.ActionsRuntimeToken);
        var resultsUrl = Environment.GetEnvironmentVariable(EnvironmentConstants.ActionsResultsUrl);
        var hasRuntimeToken = !string.IsNullOrEmpty(runtimeToken) && !string.IsNullOrEmpty(resultsUrl);

        if (!hasRuntimeToken)
        {
            Console.WriteLine("Tip: To enable automatic HTML report artifact upload, see https://tunit.dev/docs/guides/html-report#enabling-automatic-artifact-upload");
        }
        else
        {
            try
            {
                artifactId = await GitHubArtifactUploader.UploadAsync(filePath, runtimeToken!, resultsUrl!, cancellationToken);

                if (artifactId is not null)
                {
                    Console.WriteLine($"HTML report uploaded as GitHub artifact (ID: {artifactId})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to upload HTML report artifact: {ex.Message}");
            }
        }

        if (_githubReporter is not null)
        {
            if (!hasRuntimeToken)
            {
                _githubReporter.ShowArtifactUploadTip = true;
            }
            else if (artifactId is not null && !string.IsNullOrEmpty(repo) && !string.IsNullOrEmpty(runId))
            {
                var serverUrl = (Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubServerUrl) ?? EnvironmentConstants.GitHubDefaultServerUrl).TrimEnd('/');
                _githubReporter.ArtifactUrl = $"{serverUrl}/{repo}/actions/runs/{runId}/artifacts/{artifactId}";
            }
        }
    }
}
