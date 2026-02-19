using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;

namespace TUnit.Engine.Reporters;

internal class TimingReporter(IExtension extension) : IDataConsumer, ITestHostApplicationLifetime
{
    private string _outputPath = GetDefaultOutputPath();
    private bool _isEnabled;

    private readonly ConcurrentDictionary<string, List<TestNodeUpdateMessage>> _updates = [];

    public async Task<bool> IsEnabledAsync()
    {
        if (!_isEnabled)
        {
            return false;
        }

        return await extension.IsEnabledAsync();
    }

    public string Uid { get; } = $"{extension.Uid}TimingReporter";

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

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

    public async Task AfterRunAsync(int exitCode, CancellationToken cancellationToken)
    {
        if (!_isEnabled || _updates.Count == 0)
        {
            return;
        }

        var entries = new List<TimingEntry>(_updates.Count);

        foreach (var kvp in _updates)
        {
            if (kvp.Value.Count == 0)
            {
                continue;
            }

            var lastUpdate = kvp.Value[kvp.Value.Count - 1];
            var testNode = lastUpdate.TestNode;

            var timingProperty = testNode.Properties.AsEnumerable()
                .OfType<TimingProperty>()
                .FirstOrDefault();

            if (timingProperty is null)
            {
                continue;
            }

            var methodIdentifier = testNode.Properties.AsEnumerable()
                .OfType<TestMethodIdentifierProperty>()
                .FirstOrDefault();

            var stateProperty = testNode.Properties.AsEnumerable()
                .FirstOrDefault(p => p is TestNodeStateProperty) as TestNodeStateProperty;

            entries.Add(new TimingEntry
            {
                TestId = testNode.Uid.Value,
                DisplayName = testNode.DisplayName,
                Namespace = methodIdentifier?.Namespace,
                TypeName = methodIdentifier?.TypeName,
                MethodName = methodIdentifier?.MethodName,
                State = GetStateName(stateProperty),
                DurationMs = timingProperty.GlobalTiming.Duration.TotalMilliseconds,
                StartTime = timingProperty.GlobalTiming.StartTime,
                EndTime = timingProperty.GlobalTiming.EndTime
            });
        }

        var report = new TimingReport
        {
            Timestamp = DateTimeOffset.UtcNow,
            AssemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown",
            TotalTests = entries.Count,
            Tests = entries
        };

        var json = JsonSerializer.Serialize(report, TimingJsonContext.Default.TimingReport);

        await WriteJsonFileAsync(_outputPath, json, cancellationToken);
    }

    /// <summary>
    /// Enables the timing reporter. Called by the extension builder when --report-timing is specified.
    /// </summary>
    internal void Enable()
    {
        _isEnabled = true;
    }

    internal void SetOutputPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Output path cannot be null or empty", nameof(path));
        }

        _outputPath = path;
    }

    private static string GetDefaultOutputPath()
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "TestResults";
        return Path.Combine("TestResults", $"{assemblyName}-timing.json");
    }

    private static string GetStateName(TestNodeStateProperty? stateProperty)
    {
        return stateProperty switch
        {
            PassedTestNodeStateProperty => "Passed",
            FailedTestNodeStateProperty => "Failed",
            ErrorTestNodeStateProperty => "Error",
            SkippedTestNodeStateProperty => "Skipped",
            TimeoutTestNodeStateProperty => "Timeout",
            InProgressTestNodeStateProperty => "InProgress",
            _ => "Unknown"
        };
    }

    private static async Task WriteJsonFileAsync(string path, string content, CancellationToken cancellationToken)
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
                Console.WriteLine($"Timing report written to: {path}");
                return;
            }
            catch (IOException ex) when (attempt < maxAttempts && IsFileLocked(ex))
            {
                var baseDelay = 50 * Math.Pow(2, attempt - 1);
                var jitter = Random.Shared.Next(0, 50);
                var delay = (int)(baseDelay + jitter);

                Console.WriteLine($"Timing report file is locked, retrying in {delay}ms (attempt {attempt}/{maxAttempts})");
                await Task.Delay(delay, cancellationToken);
            }
        }

        Console.WriteLine($"Failed to write timing report to: {path} after {maxAttempts} attempts");
    }

    private static bool IsFileLocked(IOException exception)
    {
        var errorCode = exception.HResult & 0xFFFF;
        return errorCode == 0x20 || errorCode == 0x21 ||
               exception.Message.Contains("being used by another process") ||
               exception.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class TimingReport
{
    public DateTimeOffset Timestamp { get; set; }
    public string AssemblyName { get; set; } = "";
    public int TotalTests { get; set; }
    public List<TimingEntry> Tests { get; set; } = [];
}

internal sealed class TimingEntry
{
    public string TestId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Namespace { get; set; }
    public string? TypeName { get; set; }
    public string? MethodName { get; set; }
    public string State { get; set; } = "";
    public double DurationMs { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
}

/// <summary>
/// Source-generated JSON serialization context for AOT compatibility.
/// </summary>
[JsonSerializable(typeof(TimingReport))]
[JsonSerializable(typeof(TimingEntry))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class TimingJsonContext : JsonSerializerContext;
