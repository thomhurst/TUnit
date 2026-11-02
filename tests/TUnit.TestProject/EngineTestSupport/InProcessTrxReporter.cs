using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Platform.TestHost;

namespace TUnit.TestProject.EngineTestSupport;

/// <summary>
/// Registers <see cref="InProcessTrxReporter"/> when TUnit.Engine.Tests asks for it through
/// <see cref="InProcessTrxReporter.OutputPathVariable"/>. Other runs register nothing.
/// </summary>
public static class InProcessTrxReporterHook
{
    public static void AddExtensions(ITestApplicationBuilder builder, string[] _)
    {
        var outputPath = Environment.GetEnvironmentVariable(InProcessTrxReporter.OutputPathVariable);

        if (string.IsNullOrEmpty(outputPath))
        {
            return;
        }

        var reporter = new InProcessTrxReporter(outputPath);

        // A session lifetime handler is also registered as a data consumer; adding it again with
        // AddDataConsumer fails with "Consumer registered two time".
        builder.TestHost.AddTestSessionLifetimeHandler(serviceProvider =>
        {
            reporter.Capabilities = serviceProvider.GetService<ITestFrameworkCapabilities>();
            return reporter;
        });
        builder.TestHost.AddTestHostApplicationLifetime(_ => reporter);
    }
}

/// <summary>
/// Writes a TRX file from inside the test host, for TUnit.Engine.Tests to assert against.
/// </summary>
/// <remarks>
/// <c>--report-trx</c> always runs the test host under a test-host controller, so every run
/// starts the process twice. The engine tests start hundreds of these runs, and the second
/// start was a large part of their CI time. This reporter writes the elements and attributes
/// those tests read, with the outcome, counter and output rules of
/// Microsoft.Testing.Extensions.TrxReport 2.5 (TrxReportEngine and TrxTestResultExtractor), so
/// the existing TRX parsing and assertions apply unchanged. Unlike the controller, it cannot
/// report a test host that crashes: the file is then missing.
/// </remarks>
internal sealed class InProcessTrxReporter(string outputPath) : IDataConsumer, ITestSessionLifetimeHandler, ITestHostApplicationLifetime
{
    public const string OutputPathVariable = "TUNIT_ENGINE_TESTS_TRX_PATH";

    private const string Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private const string UnitTestType = "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b";
    private const string UncategorizedTestListId = "8c84fa94-04c1-424b-9868-57a2d4851a1d";

    private readonly object _lock = new();
    private readonly List<TestNode> _results = [];
    private DateTimeOffset _startTime = DateTimeOffset.UtcNow;

    internal ITestFrameworkCapabilities? Capabilities { get; set; }

    public string Uid => nameof(InProcessTrxReporter);

    public string Version => "1.0.0";

    public string DisplayName => "In-process TRX reporter for TUnit.Engine.Tests";

    public string Description => DisplayName;

    public Type[] DataTypesConsumed { get; } = [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public Task OnTestSessionStartingAsync(ITestSessionContext testSessionContext)
    {
        // As the TRX extension does: the framework attaches its TRX properties (exception,
        // messages, type name) to test results only once this capability is enabled.
        if (Capabilities?.GetCapability<ITrxReportCapability>() is { IsSupported: true } trxCapability)
        {
            trxCapability.Enable();
        }

        _startTime = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public Task OnTestSessionFinishingAsync(ITestSessionContext testSessionContext) => Task.CompletedTask;

    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        if (value is not TestNodeUpdateMessage message)
        {
            return Task.CompletedTask;
        }

        var state = message.TestNode.Properties.SingleOrDefault<TestNodeStateProperty>();

        // The TRX extension also drops retry attempts a later attempt supersedes. TUnit reports a
        // retried test once, with its final outcome, so there are none to drop here.
        if (state is null or DiscoveredTestNodeStateProperty or InProgressTestNodeStateProperty)
        {
            return Task.CompletedTask;
        }

        lock (_lock)
        {
            _results.Add(message.TestNode);
        }

        return Task.CompletedTask;
    }

    public Task BeforeRunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task AfterRunAsync(int exitCode, CancellationToken cancellation)
    {
        TestNode[] results;
        lock (_lock)
        {
            results = [.. _results];
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Written to a temporary file and moved into place, so a reader never sees a partial file.
        var temporaryPath = outputPath + ".tmp";
        using (var stream = File.Create(temporaryPath))
        using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true }))
        {
            Write(writer, results, exitCode);
        }

        File.Move(temporaryPath, outputPath, overwrite: true);
        return Task.CompletedTask;
    }

    private void Write(XmlWriter writer, TestNode[] results, int exitCode)
    {
        var now = DateTimeOffset.UtcNow;
        var machineName = Environment.MachineName;
        int passed = 0, failed = 0, skipped = 0, timedOut = 0;

        writer.WriteStartDocument();
        writer.WriteStartElement("TestRun", Namespace);
        writer.WriteAttributeString("id", Guid.NewGuid().ToString());
        writer.WriteAttributeString("name", $"{machineName} {now:yyyy-MM-dd HH:mm:ss}");

        writer.WriteStartElement("Times", Namespace);
        writer.WriteAttributeString("creation", Format(_startTime));
        writer.WriteAttributeString("queuing", Format(_startTime));
        writer.WriteAttributeString("start", Format(_startTime));
        writer.WriteAttributeString("finish", Format(now));
        writer.WriteEndElement();

        var definitions = new Dictionary<string, (string Name, string ExecutionId, string ClassName, string? MethodName)>();

        writer.WriteStartElement("Results", Namespace);
        foreach (var node in results)
        {
            var state = node.Properties.Single<TestNodeStateProperty>();
            var timing = node.Properties.SingleOrDefault<TimingProperty>();
            var exception = node.Properties.SingleOrDefault<TrxExceptionProperty>();
            var messages = node.Properties.SingleOrDefault<TrxMessagesProperty>();

            string outcome;
            switch (state)
            {
                case PassedTestNodeStateProperty:
                    passed++;
                    outcome = "Passed";
                    break;
                case SkippedTestNodeStateProperty:
                    skipped++;
                    outcome = "NotExecuted";
                    break;
                case TimeoutTestNodeStateProperty:
                    timedOut++;
                    outcome = "Failed";
                    break;
                default:
                    failed++;
                    outcome = "Failed";
                    break;
            }

            var testId = ToGuid(node.Uid.Value).ToString();
            var executionId = Guid.NewGuid().ToString();
            var displayName = RemoveInvalidXmlChars(node.DisplayName);

            if (!definitions.ContainsKey(testId))
            {
                var (className, methodName) = GetClassAndMethodName(node);
                definitions[testId] = (RemoveInvalidXmlChars(node.Properties.SingleOrDefault<TrxTestDefinitionName>()?.TestDefinitionName ?? node.DisplayName), executionId, className, methodName);
            }

            writer.WriteStartElement("UnitTestResult", Namespace);
            writer.WriteAttributeString("executionId", executionId);
            writer.WriteAttributeString("testId", testId);
            writer.WriteAttributeString("testName", displayName);
            writer.WriteAttributeString("computerName", machineName);
            writer.WriteAttributeString("duration", (timing?.GlobalTiming.Duration ?? TimeSpan.Zero).ToString("hh\\:mm\\:ss\\.fffffff", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("startTime", Format(timing?.GlobalTiming.StartTime ?? now));
            writer.WriteAttributeString("endTime", Format(timing?.GlobalTiming.EndTime ?? now));
            writer.WriteAttributeString("testType", UnitTestType);
            writer.WriteAttributeString("outcome", outcome);
            writer.WriteAttributeString("testListId", UncategorizedTestListId);
            writer.WriteAttributeString("relativeResultsDirectory", executionId);

            WriteOutput(writer, messages, exception);

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        writer.WriteStartElement("TestDefinitions", Namespace);
        foreach (var (id, definition) in definitions)
        {
            writer.WriteStartElement("UnitTest", Namespace);
            writer.WriteAttributeString("name", definition.Name);
            writer.WriteAttributeString("storage", outputPath);
            writer.WriteAttributeString("id", id);

            writer.WriteStartElement("Execution", Namespace);
            writer.WriteAttributeString("id", definition.ExecutionId);
            writer.WriteEndElement();

            writer.WriteStartElement("TestMethod", Namespace);
            writer.WriteAttributeString("className", definition.ClassName);
            writer.WriteAttributeString("name", definition.MethodName ?? definition.Name);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        // Mirrors TrxReportEngine: a non-zero exit code fails the run even when every test passed.
        var runFailed = exitCode != 0 || failed > 0 || timedOut > 0;

        writer.WriteStartElement("ResultSummary", Namespace);
        writer.WriteAttributeString("outcome", runFailed ? "Failed" : "Completed");

        writer.WriteStartElement("Counters", Namespace);
        WriteCounter(writer, "total", passed + failed + skipped + timedOut);
        WriteCounter(writer, "executed", passed + failed);
        WriteCounter(writer, "passed", passed);
        WriteCounter(writer, "failed", failed);
        WriteCounter(writer, "error", 0);
        WriteCounter(writer, "timeout", timedOut);
        WriteCounter(writer, "aborted", 0);
        WriteCounter(writer, "inconclusive", 0);
        WriteCounter(writer, "passedButRunAborted", 0);
        WriteCounter(writer, "notRunnable", 0);
        WriteCounter(writer, "notExecuted", skipped);
        WriteCounter(writer, "disconnected", 0);
        WriteCounter(writer, "warning", 0);
        WriteCounter(writer, "completed", 0);
        WriteCounter(writer, "inProgress", 0);
        WriteCounter(writer, "pending", 0);
        writer.WriteEndElement();

        if (exitCode != 0)
        {
            writer.WriteStartElement("RunInfos", Namespace);
            writer.WriteStartElement("RunInfo", Namespace);
            writer.WriteAttributeString("computerName", machineName);
            writer.WriteAttributeString("outcome", "Error");
            writer.WriteAttributeString("timestamp", Format(now));
            writer.WriteElementString("Text", Namespace, $"Exit code indicates failure: '{exitCode}'.");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteOutput(XmlWriter writer, TrxMessagesProperty? messages, TrxExceptionProperty? exception)
    {
        StringBuilder? standardOutput = null;
        StringBuilder? standardError = null;
        StringBuilder? debugTrace = null;

        foreach (var message in messages?.Messages ?? [])
        {
            var builder = message switch
            {
                StandardErrorTrxMessage => standardError ??= new StringBuilder(),
                DebugOrTraceTrxMessage => debugTrace ??= new StringBuilder(),
                _ => standardOutput ??= new StringBuilder(),
            };

            if (builder.Length > 0)
            {
                builder.Append(Environment.NewLine);
            }

            builder.Append(message.Message);
        }

        var hasErrorInfo = exception?.Message is not null || exception?.StackTrace is not null;

        if (standardOutput is null && standardError is null && debugTrace is null && !hasErrorInfo)
        {
            return;
        }

        writer.WriteStartElement("Output", Namespace);

        if (standardOutput is not null)
        {
            writer.WriteElementString("StdOut", Namespace, RemoveInvalidXmlChars(standardOutput.ToString()));
        }

        if (standardError is not null)
        {
            writer.WriteElementString("StdErr", Namespace, RemoveInvalidXmlChars(standardError.ToString()));
        }

        if (debugTrace is not null)
        {
            writer.WriteElementString("DebugTrace", Namespace, RemoveInvalidXmlChars(debugTrace.ToString()));
        }

        if (hasErrorInfo)
        {
            writer.WriteStartElement("ErrorInfo", Namespace);

            if (exception!.Message is { } message)
            {
                writer.WriteElementString("Message", Namespace, RemoveInvalidXmlChars(message));
            }

            if (exception.StackTrace is { } stackTrace)
            {
                writer.WriteElementString("StackTrace", Namespace, RemoveInvalidXmlChars(stackTrace));
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static (string ClassName, string? MethodName) GetClassAndMethodName(TestNode node)
    {
        var methodIdentifier = node.Properties.SingleOrDefault<TestMethodIdentifierProperty>();

        if (node.Properties.SingleOrDefault<TrxFullyQualifiedTypeNameProperty>()?.FullyQualifiedTypeName is { } typeName)
        {
            return (typeName, methodIdentifier?.MethodName);
        }

        if (methodIdentifier is null)
        {
            return ("UnknownClass", null);
        }

        var className = string.IsNullOrEmpty(methodIdentifier.Namespace)
            ? methodIdentifier.TypeName
            : $"{methodIdentifier.Namespace}.{methodIdentifier.TypeName}";

        return (className, methodIdentifier.MethodName);
    }

    private static void WriteCounter(XmlWriter writer, string name, int value)
        => writer.WriteAttributeString(name, value.ToString(CultureInfo.InvariantCulture));

    private static string Format(DateTimeOffset value) => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    // TRX ids are GUIDs; test node uids need not be. The id only has to be stable per uid.
    private static Guid ToGuid(string uid)
        => Guid.TryParse(uid, out var guid) ? guid : new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(uid)).AsSpan(0, 16));

    private static string RemoveInvalidXmlChars(string value)
    {
        StringBuilder? builder = null;

        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];
            var isValid = XmlConvert.IsXmlChar(character)
                || (char.IsHighSurrogate(character) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]));

            if (isValid)
            {
                builder?.Append(character);

                if (char.IsHighSurrogate(character))
                {
                    i++;
                    builder?.Append(value[i]);
                }
            }
            else
            {
                builder ??= new StringBuilder(value, 0, i, value.Length);
            }
        }

        return builder?.ToString() ?? value;
    }
}
