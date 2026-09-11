using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Settings;
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

    // Tests created by expanding a deferred-enumeration placeholder (or runtime variants) carry a
    // ParentTestId; surfacing it as the MTP parentTestNodeUid makes IDEs nest them under that node.
    private TestNodeUpdateMessage CreateUpdateMessage(TestContext testContext, TestNode testNode)
    {
        var parentTestId = testContext.ParentTestId;

        return parentTestId is null
            ? new TestNodeUpdateMessage(_sessionSessionUid, testNode)
            : new TestNodeUpdateMessage(_sessionSessionUid, testNode, new TestNodeUid(parentTestId));
    }

    public ValueTask Discovered(TestContext testContext)
    {
        if (testContext.IsNotDiscoverable)
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(context.MessageBus.PublishAsync(this,
            CreateUpdateMessage(testContext, testContext.ToTestNode(DiscoveredTestNodeStateProperty.CachedInstance))));
    }

    public ValueTask InProgress(TestContext testContext)
    {
        return new ValueTask(context.MessageBus.PublishAsync(this,
            CreateUpdateMessage(testContext, testContext.ToTestNode(InProgressTestNodeStateProperty.CachedInstance))));
    }

    public ValueTask Passed(TestContext testContext, DateTimeOffset start)
    {
        if (!testContext.ReportResult)
        {
            return ValueTask.CompletedTask;
        }

        var testNode = testContext.ToTestNode(PassedTestNodeStateProperty.CachedInstance);

        return new ValueTask(context.MessageBus.PublishAsync(this, CreateUpdateMessage(testContext, testNode)));
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

        return new ValueTask(context.MessageBus.PublishAsync(this, CreateUpdateMessage(testContext, testNode)));
    }

    private Exception SimplifyStacktrace(Exception exception)
    {
        // Check both the legacy --detailed-stacktrace flag and the new verbosity system
        if (commandLineOptions.IsOptionSet(DetailedStacktraceCommandProvider.DetailedStackTrace) ||
            TUnitSettings.Default.Display.DetailedStackTrace ||
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

        return new ValueTask(context.MessageBus.PublishAsync(this, CreateUpdateMessage(testContext, testNode)));
    }

    public ValueTask Cancelled(TestContext testContext, DateTimeOffset start)
    {
#pragma warning disable CS0618, MTP0001 // Retained for TUnit's own cancellation reporters
        var testNode = testContext.ToTestNode(new CancelledTestNodeStateProperty());
#pragma warning restore CS0618, MTP0001

        return new ValueTask(context.MessageBus.PublishAsync(this, CreateUpdateMessage(testContext, testNode)));
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

    private TestNodeStateProperty GetFailureStateProperty(TestContext testContext, Exception e, TimeSpan duration)
    {
        // Unwrap AggregateException once so all downstream logic sees the real cause
        var unwrapped = e is AggregateException { InnerExceptions.Count: > 0 } agg
            ? agg.InnerExceptions[0]
            : e;

        var category = FailureCategorizer.Categorize(unwrapped);
        var categoryLabel = FailureCategorizer.GetLabel(category);

        // MTP's server-mode (IDE) serializer only transmits Exception.Message and Exception.StackTrace,
        // never the InnerException chain, so Rider/VS showed just the outermost exception (#1327).
        // Fold the chain into those two members for IDE clients. Console output is left untouched:
        // MTP's terminal reporter walks InnerException itself and would otherwise print the chain twice.
        // A multi-member AggregateException (e.g. several failing [After] hooks) is folded whole so
        // every sibling is listed; a single-member one stays reduced to its real cause.
        var reported = IsConsole
            ? unwrapped
            : FlattenedException.Wrap(e is AggregateException { InnerExceptions.Count: > 1 } ? e : unwrapped);

        if (category == FailureCategory.Timeout
            && testContext.Metadata.TestDetails.Timeout != null
            && duration >= testContext.Metadata.TestDetails.Timeout.Value)
        {
            var explanation = $"[{categoryLabel}] Test timed out after {testContext.Metadata.TestDetails.Timeout.Value.TotalMilliseconds}ms";
            var diagnosticException = unwrapped.InnerException
                ?? (unwrapped is OperationCanceledException and not TaskCanceledException ? unwrapped : null);

            if (diagnosticException is not null)
            {
                // The explanation becomes error.message for IDE clients, so fold the diagnostic
                // exception's own inner messages in for them; console output walks the chain itself.
                var diagnosticMessage = IsConsole
                    ? diagnosticException.Message
                    : FlattenedException.CombineMessages(diagnosticException);

                explanation = $"{explanation}{Environment.NewLine}{diagnosticMessage}";
            }

            return new TimeoutTestNodeStateProperty(reported, explanation);
        }

        if (category == FailureCategory.Assertion)
        {
            return new FailedTestNodeStateProperty(reported, $"[{categoryLabel}] {reported.Message}");
        }

        return new ErrorTestNodeStateProperty(reported, $"[{categoryLabel}] {reported.Message}");
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
