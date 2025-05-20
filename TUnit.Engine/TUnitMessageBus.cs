﻿using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.TestHost;
using Polyfills;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Extensions;
#pragma warning disable TPEXP

namespace TUnit.Engine;

internal class TUnitMessageBus(IExtension extension, ICommandLineOptions commandLineOptions, ExecuteRequestContext context) : ITUnitMessageBus, IDataProducer
{
    private readonly SessionUid _sessionSessionUid = context.Request.Session.SessionUid;

    public async ValueTask Discovered(TestContext testContext)
    {
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(DiscoveredTestNodeStateProperty.CachedInstance)
        ));
    }
    
    public async ValueTask InProgress(TestContext testContext)
    {
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(InProgressTestNodeStateProperty.CachedInstance)
        ));
    }

    public async ValueTask Passed(TestContext testContext, DateTimeOffset start)
    {
        if (!testContext.ReportResult)
        {
            return;
        }
        
        var standardOutput = testContext.GetStandardOutput();

        var standardError = testContext.GetErrorOutput();

        var trxMessages = GetTrxMessages(testContext, standardOutput, standardError).ToArray();
        
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(PassedTestNodeStateProperty.CachedInstance)
                .WithProperty(new StandardOutputProperty(standardOutput))
                .WithProperty(new StandardErrorProperty(standardError))
                .WithProperty(GetTimingProperty(testContext, start))
                .WithProperty(new TrxMessagesProperty(trxMessages))
        ));
    }

    public async ValueTask Failed(TestContext testContext, Exception exception, DateTimeOffset start)
    {
        if (!testContext.ReportResult)
        {
            return;
        }

        var timingProperty = GetTimingProperty(testContext, start);

        if (!commandLineOptions.IsOptionSet(DetailedStacktraceCommandProvider.DetailedStackTrace))
        {
            exception = new TestFailedException(exception);
        }
        
        var updateType = GetFailureStateProperty(testContext, exception,
            timingProperty.GlobalTiming.Duration);

        var standardOutput = testContext.GetStandardOutput();

        var standardError = testContext.GetErrorOutput();

        var trxMessages = GetTrxMessages(testContext, standardOutput, standardError).ToArray();
        
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(updateType)
                .WithProperty(timingProperty)
                .WithProperty(new StandardOutputProperty(standardOutput))
                .WithProperty(new StandardErrorProperty(standardError))
                .WithProperty(new TrxExceptionProperty(exception.Message, exception.StackTrace))
                .WithProperty(new TrxMessagesProperty(trxMessages))
        ));
    }

    public async ValueTask Skipped(TestContext testContext, string reason)
    {
        var standardOutput = testContext.GetStandardOutput();

        var standardError = testContext.GetErrorOutput();
        
        var trxMessages = GetTrxMessages(testContext, standardOutput, standardError).ToArray();
        
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(new SkippedTestNodeStateProperty(reason))
                .WithProperty(new StandardOutputProperty(standardOutput))
                .WithProperty(new StandardErrorProperty(standardError))
                .WithProperty(new TrxMessagesProperty(trxMessages))
        ));
    }

    public async ValueTask Cancelled(TestContext testContext, DateTimeOffset start)
    {
        var timingProperty = GetTimingProperty(testContext, start);

        var standardOutput = testContext.GetStandardOutput();

        var standardError = testContext.GetErrorOutput();

        var trxMessages = GetTrxMessages(testContext, standardOutput, standardError).ToArray();
        
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(new CancelledTestNodeStateProperty())
                .WithProperty(timingProperty)
                .WithProperty(new StandardOutputProperty(standardOutput))
                .WithProperty(new StandardErrorProperty(standardError))
                .WithProperty(new TrxMessagesProperty(trxMessages))
        ));
    }

    public async ValueTask SessionArtifact(Artifact artifact)
    {
        await context.MessageBus.PublishAsync(this,
            new SessionFileArtifact(
                context.Request.Session.SessionUid,
                artifact.File,
                artifact.DisplayName,
                artifact.Description
            )
        );
    }

    private static TimingProperty GetTimingProperty(TestContext testContext, DateTimeOffset overallStart)
    {
        if (overallStart == default)
        {
            return new TimingProperty(new TimingInfo(default, default, TimeSpan.Zero));
        }
        
        var end = DateTimeOffset.Now;

        lock (testContext.Lock)
        {
            var stepTimings = testContext.Timings.Select(x =>
                new StepTimingInfo(x.StepName, string.Empty, new TimingInfo(x.Start, x.End, x.Duration)));

            return new TimingProperty(new TimingInfo(overallStart, end, end - overallStart), [..stepTimings]);
        }
    }

    private static IProperty GetFailureStateProperty(TestContext testContext, Exception e, TimeSpan duration)
    {
        if (testContext.TestDetails.Timeout.HasValue
            && e is TaskCanceledException or OperationCanceledException or TimeoutException
            && duration >= testContext.TestDetails.Timeout.Value)
        {
            return new TimeoutTestNodeStateProperty(e)
            {
                Timeout = testContext.TestDetails.Timeout,
            };
        }

        if (e.GetType().Name.Contains("Assertion", StringComparison.InvariantCulture))
        {
            return new FailedTestNodeStateProperty(e);
        }
        
        return new ErrorTestNodeStateProperty(e);
    }
    
    private IEnumerable<TrxMessage> GetTrxMessages(TestContext testContext, string standardOutput, string standardError)
    {
        if (!string.IsNullOrEmpty(standardOutput))
        {
            yield return new StandardOutputTrxMessage(standardOutput);
        }

        if (!string.IsNullOrEmpty(standardError))
        {
            yield return new StandardErrorTrxMessage(standardError);
        }

        if (!string.IsNullOrEmpty(testContext.SkipReason))
        {
            yield return new TrxMessage($"Skipped: {testContext.SkipReason}");
        }
    }

    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage), typeof(SessionFileArtifact)];
}