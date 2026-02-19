using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
using TUnit.Engine.Framework;

namespace TUnit.Engine.Reporters;

public class HtmlReporter(IExtension extension) : IDataConsumer, ITestHostApplicationLifetime, IFilterReceiver
{
    private string _outputPath = null!;
    private bool _isEnabled;

    public async Task<bool> IsEnabledAsync()
    {
        if (!_isEnabled)
        {
            return false;
        }

        if (string.IsNullOrEmpty(_outputPath))
        {
            _outputPath = GetDefaultOutputPath();
        }

        return await extension.IsEnabledAsync();
    }

    internal void Enable()
    {
        _isEnabled = true;
    }

    public string Uid { get; } = $"{extension.Uid}HtmlReporter";

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    private readonly ConcurrentDictionary<string, List<TestNodeUpdateMessage>> _updates = [];

    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        var testNodeUpdateMessage = (TestNodeUpdateMessage)value;

        _updates.GetOrAdd(testNodeUpdateMessage.TestNode.Uid.Value, []).Add(testNodeUpdateMessage);

        return Task.CompletedTask;
    }

    public Type[] DataTypesConsumed { get; } = [typeof(TestNodeUpdateMessage)];

    public Task BeforeRunAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task AfterRunAsync(int exitCode, CancellationToken cancellation)
    {
        if (!_isEnabled || _updates.Count == 0)
        {
            return;
        }

        // Get the last update for each test
        var lastUpdates = new Dictionary<string, TestNodeUpdateMessage>(_updates.Count);
        foreach (var kvp in _updates)
        {
            if (kvp.Value.Count > 0)
            {
                lastUpdates[kvp.Key] = kvp.Value[kvp.Value.Count - 1];
            }
        }

        var htmlContent = GenerateHtml(lastUpdates);

        if (string.IsNullOrEmpty(htmlContent))
        {
            return;
        }

        await WriteFileAsync(_outputPath, htmlContent, cancellation);
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

    private string GenerateHtml(Dictionary<string, TestNodeUpdateMessage> lastUpdates)
    {
        var passedCount = 0;
        var failedCount = 0;
        var skippedCount = 0;
        var otherCount = 0;

        foreach (var kvp in lastUpdates)
        {
            var stateProperty = kvp.Value.TestNode.Properties.AsEnumerable()
                .FirstOrDefault(p => p is TestNodeStateProperty);

            switch (stateProperty)
            {
                case PassedTestNodeStateProperty:
                    passedCount++;
                    break;
                case FailedTestNodeStateProperty or ErrorTestNodeStateProperty or TimeoutTestNodeStateProperty:
                    failedCount++;
                    break;
                case SkippedTestNodeStateProperty:
                    skippedCount++;
                    break;
                default:
                    otherCount++;
                    break;
            }
        }

        var totalCount = lastUpdates.Count;
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "TestResults";

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>Test Report - {WebUtility.HtmlEncode(assemblyName)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCss());
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"container\">");

        // Header
        sb.AppendLine($"<h1>Test Report: {WebUtility.HtmlEncode(assemblyName)}</h1>");
        sb.AppendLine($"<p class=\"timestamp\">Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");

        if (!string.IsNullOrEmpty(Filter))
        {
            sb.AppendLine($"<p class=\"filter\">Filter: <code>{WebUtility.HtmlEncode(Filter)}</code></p>");
        }

        // Summary
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<div class=\"summary-item total\"><span class=\"count\">{totalCount}</span><span class=\"label\">Total</span></div>");
        sb.AppendLine($"<div class=\"summary-item passed\"><span class=\"count\">{passedCount}</span><span class=\"label\">Passed</span></div>");
        sb.AppendLine($"<div class=\"summary-item failed\"><span class=\"count\">{failedCount}</span><span class=\"label\">Failed</span></div>");
        sb.AppendLine($"<div class=\"summary-item skipped\"><span class=\"count\">{skippedCount}</span><span class=\"label\">Skipped</span></div>");

        if (otherCount > 0)
        {
            sb.AppendLine($"<div class=\"summary-item other\"><span class=\"count\">{otherCount}</span><span class=\"label\">Other</span></div>");
        }

        sb.AppendLine("</div>");

        // Test results table
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Test</th><th>Status</th><th>Duration</th><th>Details</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var kvp in lastUpdates)
        {
            var testNode = kvp.Value.TestNode;

            var testMethodIdentifier = testNode.Properties.AsEnumerable()
                .OfType<TestMethodIdentifierProperty>()
                .FirstOrDefault();

            var className = testMethodIdentifier?.TypeName;
            var displayName = testNode.DisplayName;
            var name = string.IsNullOrEmpty(className) ? displayName : $"{className}.{displayName}";

            var stateProperty = testNode.Properties.AsEnumerable()
                .FirstOrDefault(p => p is TestNodeStateProperty);

            var status = GetStatus(stateProperty);
            var cssClass = GetStatusCssClass(stateProperty);

            var timingProperty = testNode.Properties.AsEnumerable()
                .OfType<TimingProperty>()
                .FirstOrDefault();

            var duration = timingProperty?.GlobalTiming.Duration;
            var durationText = duration.HasValue ? FormatDuration(duration.Value) : "-";

            var details = GetDetails(stateProperty);

            sb.AppendLine($"<tr class=\"{cssClass}\">");
            sb.AppendLine($"<td class=\"test-name\">{WebUtility.HtmlEncode(name)}</td>");
            sb.AppendLine($"<td class=\"status\"><span class=\"badge {cssClass}\">{WebUtility.HtmlEncode(status)}</span></td>");
            sb.AppendLine($"<td class=\"duration\">{WebUtility.HtmlEncode(durationText)}</td>");
            sb.AppendLine($"<td class=\"details\">{(string.IsNullOrEmpty(details) ? "" : $"<pre>{WebUtility.HtmlEncode(details)}</pre>")}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        sb.AppendLine("</div>"); // container
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string GetCss()
    {
        return """
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f5f5f5; color: #333; padding: 20px; }
            .container { max-width: 1200px; margin: 0 auto; }
            h1 { margin-bottom: 8px; color: #1a1a1a; }
            .timestamp { color: #666; margin-bottom: 4px; }
            .filter { color: #666; margin-bottom: 16px; }
            .filter code { background: #e8e8e8; padding: 2px 6px; border-radius: 3px; }
            .summary { display: flex; gap: 16px; margin: 20px 0; flex-wrap: wrap; }
            .summary-item { background: #fff; border-radius: 8px; padding: 16px 24px; text-align: center; box-shadow: 0 1px 3px rgba(0,0,0,0.1); min-width: 120px; }
            .summary-item .count { display: block; font-size: 2em; font-weight: bold; }
            .summary-item .label { display: block; font-size: 0.9em; color: #666; margin-top: 4px; }
            .summary-item.total { border-top: 4px solid #2196F3; }
            .summary-item.passed { border-top: 4px solid #4CAF50; }
            .summary-item.failed { border-top: 4px solid #F44336; }
            .summary-item.skipped { border-top: 4px solid #FF9800; }
            .summary-item.other { border-top: 4px solid #9E9E9E; }
            table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1); margin-top: 20px; }
            thead th { background: #fafafa; padding: 12px 16px; text-align: left; font-weight: 600; border-bottom: 2px solid #eee; }
            tbody td { padding: 10px 16px; border-bottom: 1px solid #f0f0f0; vertical-align: top; }
            tbody tr:last-child td { border-bottom: none; }
            .test-name { word-break: break-word; }
            .badge { display: inline-block; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; font-weight: 600; }
            .badge.row-passed { background: #E8F5E9; color: #2E7D32; }
            .badge.row-failed { background: #FFEBEE; color: #C62828; }
            .badge.row-skipped { background: #FFF3E0; color: #E65100; }
            .badge.row-other { background: #F5F5F5; color: #616161; }
            .details pre { white-space: pre-wrap; word-break: break-word; font-size: 0.85em; background: #f8f8f8; padding: 8px; border-radius: 4px; max-height: 200px; overflow: auto; }
            .duration { white-space: nowrap; }
            """;
    }

    private static string GetStatus(IProperty? stateProperty)
    {
        return stateProperty switch
        {
            PassedTestNodeStateProperty => "Passed",
            FailedTestNodeStateProperty => "Failed",
            ErrorTestNodeStateProperty => "Error",
            TimeoutTestNodeStateProperty => "Timed Out",
            SkippedTestNodeStateProperty => "Skipped",
#pragma warning disable CS0618 // CancelledTestNodeStateProperty is obsolete
            CancelledTestNodeStateProperty => "Cancelled",
#pragma warning restore CS0618
            InProgressTestNodeStateProperty => "In Progress",
            _ => "Unknown"
        };
    }

    private static string GetStatusCssClass(IProperty? stateProperty)
    {
        return stateProperty switch
        {
            PassedTestNodeStateProperty => "row-passed",
            FailedTestNodeStateProperty or ErrorTestNodeStateProperty or TimeoutTestNodeStateProperty => "row-failed",
            SkippedTestNodeStateProperty => "row-skipped",
            _ => "row-other"
        };
    }

    private static string GetDetails(IProperty? stateProperty)
    {
        return stateProperty switch
        {
            FailedTestNodeStateProperty failed => failed.Exception?.ToString() ?? "Test failed",
            ErrorTestNodeStateProperty error => error.Exception?.ToString() ?? "Test error",
            TimeoutTestNodeStateProperty timeout => timeout.Explanation ?? "Test timed out",
            SkippedTestNodeStateProperty skipped => skipped.Explanation ?? "",
#pragma warning disable CS0618 // CancelledTestNodeStateProperty is obsolete
            CancelledTestNodeStateProperty => "Test was cancelled",
#pragma warning restore CS0618
            _ => ""
        };
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMilliseconds < 1)
        {
            var microseconds = duration.Ticks / 10.0;
            return $"{microseconds:F0}us";
        }

        if (duration.TotalSeconds < 1)
        {
            return $"{duration.TotalMilliseconds:F0}ms";
        }

        if (duration.TotalMinutes < 1)
        {
            return $"{duration.TotalSeconds:F2}s";
        }

        return $"{duration.TotalMinutes:F1}m";
    }

    private static string GetDefaultOutputPath()
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "TestResults";
        return Path.Combine("TestResults", $"{assemblyName}-report.html");
    }

    private static async Task WriteFileAsync(string path, string content, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        const int maxAttempts = 5;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
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
                var baseDelay = 50 * Math.Pow(2, attempt - 1);
                var jitter = Random.Shared.Next(0, 50);
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
}
