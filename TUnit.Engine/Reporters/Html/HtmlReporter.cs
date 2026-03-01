using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
using TUnit.Engine.Configuration;
using TUnit.Engine.Constants;
using TUnit.Engine.Framework;

#pragma warning disable TPEXP

namespace TUnit.Engine.Reporters.Html;

internal sealed class HtmlReporter(IExtension extension) : IDataConsumer, ITestHostApplicationLifetime, IFilterReceiver
{
    private string? _outputPath;
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TestNodeUpdateMessage>> _updates = [];

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
        _updates.GetOrAdd(testNodeUpdateMessage.TestNode.Uid.Value, _ => []).Enqueue(testNodeUpdateMessage);
        return Task.CompletedTask;
    }

    public Type[] DataTypesConsumed { get; } = [typeof(TestNodeUpdateMessage)];

    public Task BeforeRunAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_outputPath))
        {
            _outputPath = GetDefaultOutputPath();
        }

#if NET
        _activityCollector = new ActivityCollector();
        _activityCollector.Start();
#endif
        return Task.CompletedTask;
    }

    public async Task AfterRunAsync(int exitCode, CancellationToken cancellation)
    {
        try
        {
#if NET
            _activityCollector?.Stop();
#endif

            if (_updates.Count == 0)
            {
                return;
            }

            var reportData = BuildReportData();
            var html = HtmlReportGenerator.GenerateHtml(reportData);

            if (string.IsNullOrEmpty(html))
            {
                return;
            }

            await WriteFileAsync(_outputPath!, html, cancellation);

            // GitHub Actions integration (artifact upload + step summary)
            await TryGitHubIntegrationAsync(_outputPath!, cancellation);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: HTML report generation failed: {ex.Message}");
        }
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

    private ReportData BuildReportData()
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "TestResults";
        var tunitVersion = typeof(HtmlReporter).Assembly.GetName().Version?.ToString() ?? "unknown";

        // Get the last update with a final state for each test
        var lastUpdates = new Dictionary<string, TestNodeUpdateMessage>(_updates.Count);
        foreach (var kvp in _updates)
        {
            TestNodeUpdateMessage? lastFinal = null;
            foreach (var update in kvp.Value)
            {
                var state = update.TestNode.Properties.SingleOrDefault<TestNodeStateProperty>();
                if (state is not null and not InProgressTestNodeStateProperty and not DiscoveredTestNodeStateProperty)
                {
                    lastFinal = update;
                }
            }

            if (lastFinal != null)
            {
                lastUpdates[kvp.Key] = lastFinal;
            }
        }

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

            // Track retry attempts by counting final-state updates.
            // A non-retried test has exactly 1 final-state update; each retry adds another.
            var retryAttempt = 0;
            if (_updates.TryGetValue(kvp.Key, out var allUpdates))
            {
                var finalStateCount = 0;
                foreach (var update in allUpdates)
                {
                    var state = update.TestNode.Properties.SingleOrDefault<TestNodeStateProperty>();
                    if (state is not null and not InProgressTestNodeStateProperty and not DiscoveredTestNodeStateProperty)
                    {
                        finalStateCount++;
                    }
                }

                if (finalStateCount > 1)
                {
                    retryAttempt = finalStateCount - 1;
                }
            }

            var testResult = ExtractTestResult(kvp.Key, testNode, traceId, spanId, retryAttempt);

            AccumulateStatus(summary, testResult.Status);

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
                AccumulateStatus(groupSummary, test.Status);
            }

            groups[i++] = new ReportTestGroup
            {
                ClassName = kvp.Key,
                Namespace = groupNamespaces.GetValueOrDefault(kvp.Key, ""),
                Summary = groupSummary,
                Tests = kvp.Value.ToArray()
            };
        }

        // Collect spans
        SpanData[]? spans = null;
#if NET
        if (_activityCollector != null)
        {
            spans = _activityCollector.GetAllSpans();
        }
#endif

        var (commitSha, branch, prNumber, repoSlug) = GetCiContext();

        return new ReportData
        {
            AssemblyName = assemblyName,
            MachineName = Environment.MachineName,
            Timestamp = DateTimeOffset.UtcNow.ToString("dd MMM yyyy, HH:mm:ss 'UTC'"),
            TUnitVersion = tunitVersion,
            OperatingSystem = RuntimeInformation.OSDescription,
            RuntimeVersion = RuntimeInformation.FrameworkDescription,
            Filter = Filter,
            TotalDurationMs = totalDurationMs,
            Summary = summary,
            Groups = groups,
            Spans = spans,
            CommitSha = commitSha,
            Branch = branch,
            PullRequestNumber = prNumber,
            RepositorySlug = repoSlug
        };
    }

    private static (string? CommitSha, string? Branch, string? PullRequestNumber, string? RepositorySlug) GetCiContext()
    {
        if (Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubActions) is not "true")
        {
            return (null, null, null, null);
        }

        var commitSha = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubSha);
        var repoSlug = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubRepository);

        // Branch: prefer GITHUB_HEAD_REF (set on PRs), fallback to GITHUB_REF (strip refs/heads/)
        var branch = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubHeadRef);
        if (string.IsNullOrEmpty(branch))
        {
            var ghRef = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubRef);
            if (ghRef is not null && ghRef.StartsWith("refs/heads/", StringComparison.Ordinal))
            {
                branch = ghRef.Substring("refs/heads/".Length);
            }
        }

        // PR number: parse from GITHUB_REF if it matches refs/pull/{n}/merge
        string? prNumber = null;
        var refValue = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubRef);
        if (refValue is not null &&
            refValue.StartsWith("refs/pull/", StringComparison.Ordinal) &&
            refValue.EndsWith("/merge", StringComparison.Ordinal))
        {
            prNumber = refValue.Substring("refs/pull/".Length, refValue.Length - "refs/pull/".Length - "/merge".Length);
        }

        return (commitSha, branch, prNumber, repoSlug);
    }

    private static void AccumulateStatus(ReportSummary summary, string status)
    {
        summary.Total++;
        switch (status)
        {
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

    private static ReportTestResult ExtractTestResult(string testId, TestNode testNode, string? traceId, string? spanId, int retryAttempt)
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
                    stdOut = o.StandardOutput;
                    break;
                case StandardErrorProperty e:
                    stdErr = e.StandardError;
                    break;
                case TestMetadataProperty meta:
                    if (string.IsNullOrEmpty(meta.Key))
                    {
                        categories ??= [];
                        categories.Add(meta.Value);
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
            StartTime = startTime?.ToString("o"),
            EndTime = endTime?.ToString("o"),
            Exception = exception,
            Output = stdOut,
            ErrorOutput = stdErr,
            Categories = categoriesArray is { Length: > 0 } ? categoriesArray : null,
            CustomProperties = customPropertiesArray is { Length: > 0 } ? customPropertiesArray : null,
            FilePath = fileLocation?.FilePath,
            LineNumber = fileLocation?.LineSpan.Start.Line,
            SkipReason = skipReason,
            RetryAttempt = retryAttempt,
            TraceId = traceId,
            SpanId = spanId
        };
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

    private static ReportExceptionData? MapException(Exception? ex)
    {
        if (ex is null)
        {
            return null;
        }

        return new ReportExceptionData
        {
            Type = ex.GetType().FullName ?? ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            InnerException = MapException(ex.InnerException)
        };
    }

    private static string GetDefaultOutputPath()
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "TestResults";
        var sanitizedName = string.Concat(assemblyName.Split(Path.GetInvalidFileNameChars()));
        var os = GetShortOsName();
        var tfm = GetShortFrameworkName();
        return Path.GetFullPath(Path.Combine("TestResults", $"{sanitizedName}-{os}-{tfm}-report.html"));
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

    private static async Task WriteFileAsync(string path, string content, CancellationToken cancellationToken)
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
                return;
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

    private static async Task TryGitHubIntegrationAsync(string filePath, CancellationToken cancellationToken)
    {
        if (Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubActions) is not "true")
        {
            return;
        }

        var summaryPath = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubStepSummary);
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

        if (hasRuntimeToken)
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

        // Write to step summary
        if (!string.IsNullOrEmpty(summaryPath))
        {
            try
            {
                var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? Path.GetFileNameWithoutExtension(filePath);
                string line;

                if (artifactId is not null && !string.IsNullOrEmpty(repo) && !string.IsNullOrEmpty(runId))
                {
                    line = $"\n\ud83d\udcca [{assemblyName} — View HTML Report](https://github.com/{repo}/actions/runs/{runId}/artifacts/{artifactId})\n";
                }
                else
                {
                    line = $"\n\ud83d\udcca **{assemblyName}** HTML report was generated — [Enable automatic artifact upload](https://tunit.dev/docs/guides/html-report#enabling-automatic-artifact-upload)\n";
                }

#if NET
                await File.AppendAllTextAsync(summaryPath, line, cancellationToken);
#else
                File.AppendAllText(summaryPath, line);
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to write GitHub step summary: {ex.Message}");
            }
        }
    }
}
