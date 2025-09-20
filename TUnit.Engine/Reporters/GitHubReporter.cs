using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
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
    private const long MaxFileSizeInBytes = 1 * 1024 * 1024; // 1MB
    private string _outputSummaryFilePath = null!;
    private GitHubReporterStyle _reporterStyle = GitHubReporterStyle.Collapsible;

    public async Task<bool> IsEnabledAsync()
    {
        if (EnvironmentVariableCache.Get("TUNIT_DISABLE_GITHUB_REPORTER") is not null ||
            EnvironmentVariableCache.Get("DISABLE_GITHUB_REPORTER") is not null)
        {
            return false;
        }

        if (EnvironmentVariableCache.Get("GITHUB_ACTIONS") is null)
        {
            return false;
        }

        if (EnvironmentVariableCache.Get("GITHUB_STEP_SUMMARY") is not { } fileName
            || !File.Exists(fileName))
        {
            return false;
        }

        _outputSummaryFilePath = fileName;

        // Determine reporter style from environment variable or default to collapsible
        var styleEnv = EnvironmentVariableCache.Get("TUNIT_GITHUB_REPORTER_STYLE");
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

        var last = _updates.ToDictionary(x => x.Key, x => x.Value.Last());

        var passedCount = last.Count(x =>
            x.Value.TestNode.Properties.AsEnumerable().Any(p => p is PassedTestNodeStateProperty));

        var failed = last.Where(x =>
            x.Value.TestNode.Properties.AsEnumerable()
                .Any(p => p is FailedTestNodeStateProperty or ErrorTestNodeStateProperty)).ToArray();

        var cancelled = last.Where(x =>
            x.Value.TestNode.Properties.AsEnumerable().Any(p => p is CancelledTestNodeStateProperty)).ToArray();

        var timeout = last
            .Where(x => x.Value.TestNode.Properties.AsEnumerable().Any(p => p is TimeoutTestNodeStateProperty))
            .ToArray();

        var skipped = last
            .Where(x => x.Value.TestNode.Properties.AsEnumerable().Any(p => p is SkippedTestNodeStateProperty))
            .ToArray();

        var inProgress = last.Where(x =>
            x.Value.TestNode.Properties.AsEnumerable().Any(p => p is InProgressTestNodeStateProperty)).ToArray();

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"### {Assembly.GetEntryAssembly()?.GetName().Name} ({targetFramework})");

        if (!string.IsNullOrEmpty(Filter))
        {
            stringBuilder.AppendLine($"#### Filter: `{Filter}`");
        }

        stringBuilder.AppendLine();
        stringBuilder.AppendLine("| Test Count | Status |");
        stringBuilder.AppendLine("| --- | --- |");
        stringBuilder.AppendLine($"| {passedCount} | Passed |");
        stringBuilder.AppendLine($"| {failed.Length} | Failed |");

        if (skipped.Length > 0)
        {
            stringBuilder.AppendLine($"| {skipped.Length} | Skipped |");
        }

        if (timeout.Length > 0)
        {
            stringBuilder.AppendLine($"| {timeout.Length} | Timed Out |");
        }

        if (cancelled.Length > 0)
        {
            stringBuilder.AppendLine($"| {cancelled.Length} | Cancelled |");
        }

        if (inProgress.Length > 0)
        {
            stringBuilder.AppendLine($"| {inProgress.Length} | In Progress (never completed) |");
        }

        if (passedCount == last.Count)
        {
            return WriteFile(stringBuilder.ToString());
        }

        // Build the details table
        var detailsBuilder = new StringBuilder();
        detailsBuilder.AppendLine();
        detailsBuilder.AppendLine();
        detailsBuilder.AppendLine("### Details");
        detailsBuilder.AppendLine();
        detailsBuilder.AppendLine("| Test | Status | Details | Duration |");
        detailsBuilder.AppendLine("| --- | --- | --- | --- |");

        foreach (var testNodeUpdateMessage in last.Values)
        {
            var name = testNodeUpdateMessage.TestNode.DisplayName;

            var passedProperty = testNodeUpdateMessage.TestNode.Properties.OfType<PassedTestNodeStateProperty>().FirstOrDefault();

            if (passedProperty != null)
            {
                continue;
            }

            var stateProperty = testNodeUpdateMessage.TestNode.Properties.AsEnumerable().FirstOrDefault(p => p is TestNodeStateProperty);

            var status = GetStatus(stateProperty);

            var details = GetDetails(stateProperty, testNodeUpdateMessage.TestNode.Properties).Replace("\n", " <br> ");

            var timingProperty = testNodeUpdateMessage.TestNode.Properties.AsEnumerable().OfType<TimingProperty>().FirstOrDefault();

            var duration = timingProperty?.GlobalTiming.Duration;

            detailsBuilder.AppendLine($"| {name} | {status} | {details} | {duration} |");
        }

        // Wrap in collapsible section if using collapsible style
        if (_reporterStyle == GitHubReporterStyle.Collapsible)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("<details>");
            stringBuilder.AppendLine("<summary>📊 Test Details (click to expand)</summary>");
            stringBuilder.Append(detailsBuilder.ToString());
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("</details>");
        }
        else
        {
            // Full style - append details directly
            stringBuilder.Append(detailsBuilder.ToString());
        }

        return WriteFile(stringBuilder.ToString());
    }

    private Task WriteFile(string contents)
    {
        var fileInfo = new FileInfo(_outputSummaryFilePath);
        var currentFileSize = fileInfo.Exists ? fileInfo.Length : 0;
        long newContentSize = Encoding.UTF8.GetByteCount(contents);
        var newSize = currentFileSize + newContentSize;

        if (newSize > MaxFileSizeInBytes)
        {
            Console.WriteLine("Appending to the GitHub Step Summary would exceed the 1MB file size limit.");
            return Task.CompletedTask;
        }

#if NET
        return File.AppendAllTextAsync(_outputSummaryFilePath, contents, Encoding.UTF8);
#else
        File.AppendAllText(_outputSummaryFilePath, contents, Encoding.UTF8);

        return Task.CompletedTask;
#endif
    }

    private string GetDetails(IProperty? stateProperty, PropertyBag properties)
    {
        if (stateProperty is FailedTestNodeStateProperty
            or ErrorTestNodeStateProperty
            or TimeoutTestNodeStateProperty
            or CancelledTestNodeStateProperty)
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
                failedTestNodeStateProperty.Exception?.ToString() ?? "Test failed",
            ErrorTestNodeStateProperty errorTestNodeStateProperty =>
                errorTestNodeStateProperty.Exception?.ToString() ?? "Test failed",
            TimeoutTestNodeStateProperty timeoutTestNodeStateProperty => timeoutTestNodeStateProperty.Explanation,
            CancelledTestNodeStateProperty => "Test was cancelled",
            _ => null
        };
    }

    private static string GetStatus(IProperty? stateProperty)
    {
        return stateProperty switch
        {
            CancelledTestNodeStateProperty => "Cancelled",
            ErrorTestNodeStateProperty => "Failed",
            FailedTestNodeStateProperty => "Failed",
            InProgressTestNodeStateProperty => "In Progress (never finished)",
            PassedTestNodeStateProperty => "Passed",
            SkippedTestNodeStateProperty => "Skipped",
            TimeoutTestNodeStateProperty => "Timed Out",
            _ => "Unknown"
        };
    }

    public string? Filter { get; set; }

    internal void SetReporterStyle(GitHubReporterStyle style)
    {
        _reporterStyle = style;
    }
}
