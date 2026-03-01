using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
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

internal class HtmlReporter(IExtension extension) : IDataConsumer, ITestHostApplicationLifetime, IFilterReceiver
{
    private string? _outputPath;
    private readonly ConcurrentDictionary<string, ConcurrentBag<TestNodeUpdateMessage>> _updates = [];

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
        _updates.GetOrAdd(testNodeUpdateMessage.TestNode.Uid.Value, _ => []).Add(testNodeUpdateMessage);
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

            // GitHub Actions artifact upload
            await TryUploadGitHubArtifactAsync(_outputPath!, cancellation);
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
            var testResult = ExtractTestResult(kvp.Key, testNode);

            // Correlate trace/span IDs from collected activities
            if (spanLookup?.TryGetValue(kvp.Key, out var spanInfo) == true)
            {
                testResult.TraceId = spanInfo.TraceId;
                testResult.SpanId = spanInfo.SpanId;
            }

            // Track retry attempts by counting final-state updates.
            // A non-retried test has exactly 1 final-state update; each retry adds another.
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
                    testResult.RetryAttempt = finalStateCount - 1;
                }
            }

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

        return new ReportData
        {
            AssemblyName = assemblyName,
            MachineName = Environment.MachineName,
            Timestamp = DateTimeOffset.UtcNow.ToString("o"),
            TUnitVersion = tunitVersion,
            Filter = Filter,
            TotalDurationMs = totalDurationMs,
            Summary = summary,
            Groups = groups,
            Spans = spans
        };
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

    private static ReportTestResult ExtractTestResult(string testId, TestNode testNode)
    {
        var stateProperty = testNode.Properties.AsEnumerable()
            .FirstOrDefault(p => p is TestNodeStateProperty);

        var testMethodIdentifier = testNode.Properties.AsEnumerable()
            .OfType<TestMethodIdentifierProperty>()
            .FirstOrDefault();

        var timingProperty = testNode.Properties.AsEnumerable()
            .OfType<TimingProperty>()
            .FirstOrDefault();

        var fileLocation = testNode.Properties.AsEnumerable()
            .OfType<TestFileLocationProperty>()
            .FirstOrDefault();

        var stdOut = testNode.Properties.AsEnumerable()
            .OfType<StandardOutputProperty>()
            .FirstOrDefault()?.StandardOutput;

        var stdErr = testNode.Properties.AsEnumerable()
            .OfType<StandardErrorProperty>()
            .FirstOrDefault()?.StandardError;

        var categories = testNode.Properties.AsEnumerable()
            .OfType<TestMetadataProperty>()
            .Where(p => string.IsNullOrEmpty(p.Key))
            .Select(p => p.Value)
            .ToArray();

        var customProperties = testNode.Properties.AsEnumerable()
            .OfType<TestMetadataProperty>()
            .Where(p => !string.IsNullOrEmpty(p.Key))
            .Select(p => new ReportKeyValue { Key = p.Key, Value = p.Value })
            .ToArray();

        var className = testMethodIdentifier?.TypeName ?? "UnknownClass";
        var methodName = testMethodIdentifier?.MethodName ?? testNode.DisplayName;

        var (status, exception, skipReason) = ExtractStatus(stateProperty);

        var durationMs = timingProperty?.GlobalTiming.Duration.TotalMilliseconds ?? 0;
        var startTime = timingProperty?.GlobalTiming.StartTime;
        var endTime = startTime.HasValue ? startTime.Value + timingProperty!.GlobalTiming.Duration : (DateTimeOffset?)null;

        ReportTimingStep[]? timingSteps = null;
        if (timingProperty?.StepTimings is { Length: > 0 } steps)
        {
            timingSteps = new ReportTimingStep[steps.Length];
            for (var i = 0; i < steps.Length; i++)
            {
                timingSteps[i] = new ReportTimingStep
                {
                    Name = steps[i].Id,
                    DurationMs = steps[i].Timing.Duration.TotalMilliseconds
                };
            }
        }

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
            Categories = categories.Length > 0 ? categories : null,
            CustomProperties = customProperties.Length > 0 ? customProperties : null,
            FilePath = fileLocation?.FilePath,
            LineNumber = fileLocation?.LineSpan.Start.Line,
            SkipReason = skipReason,
            TimingSteps = timingSteps
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
        return Path.GetFullPath(Path.Combine("TestResults", $"{sanitizedName}-report.html"));
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
        var errorCode = exception.HResult & 0xFFFF;
        return errorCode == 0x20 || errorCode == 0x21 ||
               exception.Message.Contains("being used by another process") ||
               exception.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task TryUploadGitHubArtifactAsync(string filePath, CancellationToken cancellationToken)
    {
        if (Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubActions) is not "true")
        {
            return;
        }

        var runtimeToken = Environment.GetEnvironmentVariable(EnvironmentConstants.ActionsRuntimeToken);
        var resultsUrl = Environment.GetEnvironmentVariable(EnvironmentConstants.ActionsResultsUrl);

        if (string.IsNullOrEmpty(runtimeToken))
        {
            Console.WriteLine("Warning: ACTIONS_RUNTIME_TOKEN not set — skipping HTML report artifact upload");
            return;
        }

        if (string.IsNullOrEmpty(resultsUrl))
        {
            Console.WriteLine("Warning: ACTIONS_RESULTS_URL not set — skipping HTML report artifact upload");
            return;
        }

        try
        {
            var artifactId = await GitHubArtifactUploader.UploadAsync(filePath, runtimeToken, resultsUrl, cancellationToken);

            if (artifactId is null)
            {
                Console.WriteLine("Warning: HTML report artifact upload returned no artifact ID");
                return;
            }

            Console.WriteLine($"HTML report uploaded as GitHub artifact (ID: {artifactId})");

            var repo = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubRepository);
            var runId = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubRunId);
            var summaryPath = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubStepSummary);

            if (!string.IsNullOrEmpty(summaryPath) && !string.IsNullOrEmpty(repo) && !string.IsNullOrEmpty(runId))
            {
                var link = $"\n📊 [View HTML Test Report](https://github.com/{repo}/actions/runs/{runId}/artifacts/{artifactId})\n";
#if NET
                await File.AppendAllTextAsync(summaryPath, link, cancellationToken);
#else
                File.AppendAllText(summaryPath, link);
#endif
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to upload HTML report to GitHub Actions: {ex.Message}");
        }
    }
}
