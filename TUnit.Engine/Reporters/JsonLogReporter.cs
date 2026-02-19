using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;

#pragma warning disable TPEXP

namespace TUnit.Engine.Reporters;

/// <summary>
/// A reporter that writes structured JSON log lines (one per event) to a file.
/// Each line represents a test lifecycle event (start, end, skip) in a format
/// suitable for consumption by observability tools.
/// </summary>
/// <remarks>
/// Output format (JSON Lines / .jsonl):
/// <code>
/// {"event":"test_start","testId":"...","testName":"...","timestamp":"..."}
/// {"event":"test_end","testId":"...","testName":"...","status":"passed","duration_ms":123,"timestamp":"..."}
/// {"event":"test_skip","testId":"...","testName":"...","reason":"...","timestamp":"..."}
/// </code>
/// </remarks>
internal sealed class JsonLogReporter(IExtension extension) : IDataConsumer, ITestHostApplicationLifetime, IAsyncDisposable
{
    private string _outputPath = null!;
    private bool _isEnabled;
    private StreamWriter? _writer;
    private readonly Lock _lock = new();

    public string Uid { get; } = $"{extension.Uid}JsonLogReporter";

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    public Task<bool> IsEnabledAsync()
    {
        return Task.FromResult(_isEnabled);
    }

    public Type[] DataTypesConsumed { get; } = [typeof(TestNodeUpdateMessage)];

    internal void Enable(string? outputPath)
    {
        _isEnabled = true;
        _outputPath = outputPath ?? GetDefaultOutputPath();
    }

    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        if (!_isEnabled)
        {
            return Task.CompletedTask;
        }

        var message = (TestNodeUpdateMessage)value;
        var testNode = message.TestNode;
        var testId = testNode.Uid.Value;
        var testName = testNode.DisplayName;
        var stateProperty = GetStateProperty(testNode.Properties);

        if (stateProperty is null)
        {
            return Task.CompletedTask;
        }

        var jsonLine = stateProperty switch
        {
            InProgressTestNodeStateProperty => BuildTestStartJson(testId, testName),
            PassedTestNodeStateProperty => BuildTestEndJson(testId, testName, "passed", testNode.Properties),
            FailedTestNodeStateProperty => BuildTestEndJson(testId, testName, "failed", testNode.Properties),
            ErrorTestNodeStateProperty => BuildTestEndJson(testId, testName, "error", testNode.Properties),
            TimeoutTestNodeStateProperty => BuildTestEndJson(testId, testName, "timeout", testNode.Properties),
            SkippedTestNodeStateProperty skipped => BuildTestSkipJson(testId, testName, skipped.Explanation),
            _ => null
        };

        if (jsonLine is null)
        {
            return Task.CompletedTask;
        }

        WriteJsonLine(jsonLine);

        return Task.CompletedTask;
    }

    public Task BeforeRunAsync(CancellationToken cancellationToken)
    {
        if (!_isEnabled)
        {
            return Task.CompletedTask;
        }

        EnsureWriterCreated();
        return Task.CompletedTask;
    }

    public Task AfterRunAsync(int exitCode, CancellationToken cancellationToken)
    {
        if (!_isEnabled || _writer is null)
        {
            return Task.CompletedTask;
        }

        lock (_lock)
        {
            _writer.Flush();
        }

        Console.WriteLine($"JSON log report written to: {_outputPath}");

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_writer is not null)
        {
            await _writer.FlushAsync().ConfigureAwait(false);
#if NET
            await _writer.DisposeAsync().ConfigureAwait(false);
#else
            _writer.Dispose();
#endif
            _writer = null;
        }
    }

    private void EnsureWriterCreated()
    {
        if (_writer is not null)
        {
            return;
        }

        var directory = Path.GetDirectoryName(_outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(_outputPath, append: false, encoding: Encoding.UTF8)
        {
            AutoFlush = false
        };
    }

    private void WriteJsonLine(string jsonLine)
    {
        lock (_lock)
        {
            EnsureWriterCreated();
            _writer!.WriteLine(jsonLine);
            _writer.Flush();
        }
    }

    private static string BuildTestStartJson(string testId, string testName)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteString("event", "test_start");
        writer.WriteString("testId", testId);
        writer.WriteString("testName", testName);
        writer.WriteString("timestamp", DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string BuildTestEndJson(string testId, string testName, string status, PropertyBag properties)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteString("event", "test_end");
        writer.WriteString("testId", testId);
        writer.WriteString("testName", testName);
        writer.WriteString("status", status);

        var timingProperty = FindProperty<TimingProperty>(properties);
        if (timingProperty is not null)
        {
            var durationMs = timingProperty.GlobalTiming.Duration.TotalMilliseconds;
            writer.WriteNumber("duration_ms", Math.Round(durationMs, 2));
        }

        writer.WriteString("timestamp", DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string BuildTestSkipJson(string testId, string testName, string? reason)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteString("event", "test_skip");
        writer.WriteString("testId", testId);
        writer.WriteString("testName", testName);
        writer.WriteString("reason", reason ?? "Skipped");
        writer.WriteString("timestamp", DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static T? FindProperty<T>(PropertyBag properties) where T : class, IProperty
    {
        foreach (var property in properties.AsEnumerable())
        {
            if (property is T match)
            {
                return match;
            }
        }

        return null;
    }

    private static TestNodeStateProperty? GetStateProperty(PropertyBag properties)
    {
        foreach (var property in properties.AsEnumerable())
        {
            if (property is TestNodeStateProperty stateProperty)
            {
                return stateProperty;
            }
        }

        return null;
    }

    private static string GetDefaultOutputPath()
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "TestResults";
        return Path.Combine("TestResults", $"{assemblyName}-log.jsonl");
    }
}
