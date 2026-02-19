using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Enums;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Extensions;
using TUnit.Engine.Services;

#pragma warning disable TPEXP

namespace TUnit.Engine;

internal class TUnitMessageBus(IExtension extension, ICommandLineOptions commandLineOptions, VerbosityService verbosityService, IServiceProvider serviceProvider, ExecuteRequestContext context) : ITUnitMessageBus, IDataProducer
{
    private static readonly Type[] _dataTypesProduced = [typeof(TestNodeUpdateMessage), typeof(SessionFileArtifact)];

    private readonly SessionUid _sessionSessionUid = context.Request.Session.SessionUid;

    private bool? _isConsole;
    private bool IsConsole => _isConsole ??= serviceProvider.GetClientInfo().Id.Contains("console", StringComparison.InvariantCultureIgnoreCase);

    public ValueTask Discovered(TestContext testContext)
    {
        if (testContext.IsNotDiscoverable)
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode(DiscoveredTestNodeStateProperty.CachedInstance)
        )));
    }

    public ValueTask InProgress(TestContext testContext)
    {
        return new ValueTask(context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode(InProgressTestNodeStateProperty.CachedInstance)
        )));
    }

    public ValueTask Passed(TestContext testContext, DateTimeOffset start)
    {
        if (!testContext.ReportResult)
        {
            return ValueTask.CompletedTask;
        }

        var testNode = testContext.ToTestNode(PassedTestNodeStateProperty.CachedInstance);

        return new ValueTask(context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testNode
        )));
    }

    public ValueTask Failed(TestContext testContext, Exception exception, DateTimeOffset start)
    {
        if (!testContext.ReportResult)
        {
            return ValueTask.CompletedTask;
        }

        exception = SimplifyStacktrace(exception);

        var duration = testContext.Execution.TestEnd - testContext.Execution.TestStart;

        var updateType = GetFailureStateProperty(testContext, exception, duration ?? TimeSpan.Zero);

        var testNode = testContext.ToTestNode(updateType);

        return new ValueTask(context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testNode
        )));
    }

    private Exception SimplifyStacktrace(Exception exception)
    {
        // Check both the legacy --detailed-stacktrace flag and the new verbosity system
        if (commandLineOptions.IsOptionSet(DetailedStacktraceCommandProvider.DetailedStackTrace) ||
            verbosityService?.ShowDetailedStackTrace == true)
        {
            return exception;
        }

        if (IsConsole)
        {
            // It's only really spammy in a console environment.
            // In an IDE, every test has their own output window, so it's not as spammy.
            return new TestFailedException(exception);
        }

        return exception;
    }

    public ValueTask Skipped(TestContext testContext, string reason)
    {
        var testNode = testContext.ToTestNode(new SkippedTestNodeStateProperty(reason));

        return new ValueTask(context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testNode
        )));
    }

    public ValueTask Cancelled(TestContext testContext, DateTimeOffset start)
    {
#pragma warning disable CS0618 // CancelledTestNodeStateProperty is obsolete - TUnit still needs to report cancelled state for its own reporters
        var testNode = testContext.ToTestNode(new CancelledTestNodeStateProperty());
#pragma warning restore CS0618

        return new ValueTask(context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testNode
        )));
    }

    public ValueTask SessionArtifact(Artifact artifact)
    {
        return new ValueTask(context.MessageBus.PublishAsync(this,
            new SessionFileArtifact(
                context.Request.Session.SessionUid,
                artifact.File,
                artifact.DisplayName,
                artifact.Description
            )
        ));
    }

    public ValueTask PublishOutputUpdate(TestNode testNode)
    {
        return new ValueTask(context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testNode
        )));
    }

    private static TestNodeStateProperty GetFailureStateProperty(TestContext testContext, Exception e, TimeSpan duration)
    {
        // Unwrap AggregateException once so all downstream logic sees the real cause
        var unwrapped = e is AggregateException { InnerExceptions.Count: > 0 } agg
            ? agg.InnerExceptions[0]
            : e;

        var category = FailureCategorizer.Categorize(unwrapped);
        var categoryLabel = FailureCategorizer.GetLabel(category);

        if (category == FailureCategory.Timeout
            && testContext.Metadata.TestDetails.Timeout != null
            && duration >= testContext.Metadata.TestDetails.Timeout.Value)
        {
            return new TimeoutTestNodeStateProperty($"[{categoryLabel}] Test timed out after {testContext.Metadata.TestDetails.Timeout.Value.TotalMilliseconds}ms");
        }

        if (category == FailureCategory.Assertion)
        {
            return new FailedTestNodeStateProperty(unwrapped, $"[{categoryLabel}] {unwrapped.Message}");
        }

        return new ErrorTestNodeStateProperty(unwrapped, $"[{categoryLabel}] {unwrapped.Message}");
    }

    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    public Type[] DataTypesProduced => _dataTypesProduced;
}
