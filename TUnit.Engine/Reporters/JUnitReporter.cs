using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
using TUnit.Engine.Framework;
using TUnit.Engine.Xml;

namespace TUnit.Engine.Reporters;

public class JUnitReporter(IExtension extension) : IDataConsumer, ITestHostApplicationLifetime, IFilterReceiver
{
    private string _outputPath = null!;
    private bool _isEnabled;

    public async Task<bool> IsEnabledAsync()
    {
        // Check if explicitly disabled
        if (Environment.GetEnvironmentVariable("TUNIT_DISABLE_JUNIT_REPORTER") is not null)
        {
            return false;
        }

        // Check if explicitly enabled OR running in GitLab CI
        var explicitlyEnabled = Environment.GetEnvironmentVariable("TUNIT_ENABLE_JUNIT_REPORTER") is not null;
        var runningInGitLab = Environment.GetEnvironmentVariable("GITLAB_CI") is not null ||
                              Environment.GetEnvironmentVariable("CI_SERVER") is not null;

        if (!explicitlyEnabled && !runningInGitLab)
        {
            return false;
        }

        // Determine output path (only if not already set via command-line argument)
        if (string.IsNullOrEmpty(_outputPath))
        {
            _outputPath = Environment.GetEnvironmentVariable("JUNIT_XML_OUTPUT_PATH")
                ?? GetDefaultOutputPath();
        }

        _isEnabled = true;
        return await extension.IsEnabledAsync();
    }

    public string Uid { get; } = $"{extension.Uid}JUnitReporter";

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
        var lastUpdates = new List<TestNodeUpdateMessage>(_updates.Count);
        foreach (var kvp in _updates.Where(kvp => kvp.Value.Count > 0))
        {
            lastUpdates.Add(kvp.Value[kvp.Value.Count - 1]);
        }

        // Generate JUnit XML
        var xmlContent = JUnitXmlWriter.GenerateXml(lastUpdates, Filter);

        if (string.IsNullOrEmpty(xmlContent))
        {
            return;
        }

        // Write to file with retry logic
        await WriteXmlFileAsync(_outputPath, xmlContent, cancellation);
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

    private static string GetDefaultOutputPath()
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "TestResults";
        return Path.Combine("TestResults", $"{assemblyName}-junit.xml");
    }

    private static async Task WriteXmlFileAsync(string path, string content, CancellationToken cancellationToken)
    {
        // Ensure directory exists
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
                Console.WriteLine($"JUnit XML report written to: {path}");
                return;
            }
            catch (IOException ex) when (attempt < maxAttempts && IsFileLocked(ex))
            {
                var baseDelay = 50 * Math.Pow(2, attempt - 1);
                var jitter = Random.Shared.Next(0, 50);
                var delay = (int)(baseDelay + jitter);

                Console.WriteLine($"JUnit XML file is locked, retrying in {delay}ms (attempt {attempt}/{maxAttempts})");
                await Task.Delay(delay, cancellationToken);
            }
        }

        Console.WriteLine($"Failed to write JUnit XML report to: {path} after {maxAttempts} attempts");
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
}
