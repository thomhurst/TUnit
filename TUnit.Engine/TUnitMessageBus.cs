using System.Runtime.CompilerServices;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.TestHost;
using Polyfills;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Extensions;
#pragma warning disable TPEXP

namespace TUnit.Engine;

public class TUnitMessageBus(IExtension extension, ICommandLineOptions commandLineOptions, ExecuteRequestContext context) : ITUnitMessageBus, IDataProducer
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
        
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(PassedTestNodeStateProperty.CachedInstance)
                .WithProperty(new StandardOutputProperty(testContext.GetStandardOutput()))
                .WithProperty(new StandardErrorProperty(testContext.GetErrorOutput()))
                .WithProperty(GetTimingProperty(testContext, start))
        ));
    }

    public async ValueTask Failed(TestContext testContext, Exception exception, DateTimeOffset start)
    {
        if (!testContext.ReportResult)
        {
            return;
        }

        var timingProperty = GetTimingProperty(testContext, start);

        TidyStacktrace(exception);

        var updateType = GetFailureStateProperty(testContext, exception,
            timingProperty.GlobalTiming.Duration);

        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(updateType)
                .WithProperty(timingProperty)
                .WithProperty(new StandardOutputProperty(testContext.GetStandardOutput()))
                .WithProperty(new StandardErrorProperty(testContext.GetErrorOutput()))
                .WithProperty(new TrxExceptionProperty(exception.Message, exception.StackTrace))
        ));
    }

    public async ValueTask Skipped(TestContext testContext, string reason)
    {
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(new SkippedTestNodeStateProperty(reason))
                .WithProperty(new StandardOutputProperty(testContext.GetStandardOutput()))
                .WithProperty(new StandardErrorProperty(testContext.GetErrorOutput()))
        ));
    }

    public async ValueTask Cancelled(TestContext testContext)
    {
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(new CancelledTestNodeStateProperty())
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

    public async ValueTask TestArtifact(TestContext testContext, Artifact artifact)
    {
        await context.MessageBus.PublishAsync(this,
            new TestNodeFileArtifact(
                context.Request.Session.SessionUid,
                testContext.ToTestNode(),
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

    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage), typeof(SessionFileArtifact), typeof(TestNodeFileArtifact)];

    private string FilterStackTrace(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
        {
            return string.Empty;
        }
        
        var lines = stackTrace!.Split([Environment.NewLine], StringSplitOptions.None);

        return string.Join(Environment.NewLine,
            lines.TakeWhile(x => !x.Trim().StartsWith("at TUnit")));
    }

    private void TidyStacktrace(Exception exception)
    {
        if (commandLineOptions.IsOptionSet(DetailedStacktraceCommandProvider.DetailedStackTrace))
        {
            return;
        }

#if NET8_0_OR_GREATER
        try
        {
            // Remove TUnit from the stack trace so that it shows just the user code and is more readable
            var originalStacktrace = exception.StackTrace;
            
            var newStackTrace = FilterStackTrace(originalStacktrace);
            
            StackTrace(exception) = null;
            RemoteStackTraceString(exception) = null;
            StackTraceString(exception) = newStackTrace;

            if (exception.InnerException != null)
            {
                TidyStacktrace(exception.InnerException);
            }

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_stackTraceString")]
            static extern ref string? StackTraceString(Exception e);
            
            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_remoteStackTraceString")]
            static extern ref string? RemoteStackTraceString(Exception e);

#if NET8_0
            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_stackTrace")]
            static extern ref byte[]? StackTrace(Exception e);    
#else
            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_stackTrace")]
            static extern ref object? StackTrace(Exception e);   
#endif
        }
        catch
        {
            // Ignore and use original stack trace
        }
#endif
    }
}