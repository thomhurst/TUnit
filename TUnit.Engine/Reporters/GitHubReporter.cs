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
        detailsBuilder.AppendLine("""<table role="table" tabindex="0">""");
        detailsBuilder.AppendLine("<thead><tr><th>Test</th><th>Status</th><th>Details</th><th>Duration</th></tr></thead>");
        detailsBuilder.AppendLine("<tbody>");

        foreach (var testNodeUpdateMessage in last.Values)
        {
            var testMethodIdentifier = testNodeUpdateMessage.TestNode.Properties.AsEnumerable()
                .OfType<TestMethodIdentifierProperty>()
                .FirstOrDefault();

            var className = testMethodIdentifier?.TypeName;
            var displayName = testNodeUpdateMessage.TestNode.DisplayName;
            var name = string.IsNullOrEmpty(className) ? displayName : $"{className}.{displayName}";

            var passedProperty = testNodeUpdateMessage.TestNode.Properties.OfType<PassedTestNodeStateProperty>().FirstOrDefault();

            if (passedProperty != null)
            {
                continue;
            }

            var stateProperty = testNodeUpdateMessage.TestNode.Properties.AsEnumerable().FirstOrDefault(p => p is TestNodeStateProperty);

            var status = GetStatus(stateProperty);

            var details = GetDetails(stateProperty, testNodeUpdateMessage.TestNode.Properties);

            var timingProperty = testNodeUpdateMessage.TestNode.Properties.AsEnumerable().OfType<TimingProperty>().FirstOrDefault();

            var duration = timingProperty?.GlobalTiming.Duration;

            detailsBuilder.AppendLine("<tr>");
            detailsBuilder.AppendLine($"<td>{name}</td>");
            detailsBuilder.AppendLine($"<td>{status}</td>");
            detailsBuilder.AppendLine($"<td>{details}</td>");
            detailsBuilder.AppendLine($"<td>{duration}</td>");
            detailsBuilder.AppendLine("</tr>");
        }
        detailsBuilder.AppendLine("</tbody></table>");

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
                failedTestNodeStateProperty.Exception?.ToString() ?? "Test failed",
            ErrorTestNodeStateProperty errorTestNodeStateProperty =>
                errorTestNodeStateProperty.Exception?.ToString() ?? "Test failed",
            TimeoutTestNodeStateProperty timeoutTestNodeStateProperty => timeoutTestNodeStateProperty.Explanation,
#pragma warning disable CS0618 // CancelledTestNodeStateProperty is obsolete
            CancelledTestNodeStateProperty => "Test was cancelled",
#pragma warning restore CS0618
            _ => null
        };
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

    public string? Filter { get; set; }

    internal void SetReporterStyle(GitHubReporterStyle style)
    {
        _reporterStyle = style;
    }
}
