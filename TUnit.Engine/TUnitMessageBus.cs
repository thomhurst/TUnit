using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine;

public class TUnitMessageBus(IExtension extension, ExecuteRequestContext context) : ITUnitMessageBus, IDataProducer
{
    private readonly SessionUid _sessionSessionUid = context.Request.Session.SessionUid;

    public Task Discovered(TestContext testContext)
    {
        return context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(DiscoveredTestNodeStateProperty.CachedInstance)
        ));
    }
    
    public Task InProgress(TestContext testContext)
    {
        return context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(InProgressTestNodeStateProperty.CachedInstance)
        ));
    }

    public Task Passed(TestContext testContext)
    {
        return context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(PassedTestNodeStateProperty.CachedInstance)
        ));
    }

    public Task Failed(TestContext testContext, Exception exception)
    {
        return context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(new FailedTestNodeStateProperty(exception))
        ));
    }

    public Task FailedInitialization(FailedInitializationTest failedInitializationTest)
    {
        var testClass = failedInitializationTest.TestClass;
        
        return context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: new TestNode
                {
                    Uid = failedInitializationTest.TestId,
                    DisplayName = $"{failedInitializationTest.TestName} (Failed Initialization)",
                    Properties = new PropertyBag
                        (
                            new TestFileLocationProperty(failedInitializationTest.TestFilePath,
                                new LinePositionSpan(new LinePosition(failedInitializationTest.TestLineNumber, 0),
                                    new LinePosition(failedInitializationTest.TestLineNumber, 0))
                            ),
                            new TestMethodIdentifierProperty(
                                AssemblyFullName: testClass.Assembly.FullName!,
                                Namespace: testClass.Namespace!,
                                TypeName: testClass.Name,
                                MethodName: failedInitializationTest.TestName,
                                ParameterTypeFullNames: failedInitializationTest.ParameterTypeFullNames.Select(x => x.FullName!).ToArray(),
                                ReturnTypeFullName: failedInitializationTest.ReturnType.FullName!
                            ),
                            // TRX Reports
                            new TrxExceptionProperty(failedInitializationTest.Exception.Message, failedInitializationTest.Exception.StackTrace)
                            )
                }
                .WithProperty(new ErrorTestNodeStateProperty(failedInitializationTest.Exception))
        ));
    }

    public Task Errored(TestContext testContext, Exception exception)
    {
        return context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(new ErrorTestNodeStateProperty(exception))
        ));
    }

    public Task Skipped(TestContext testContext, string reason)
    {
        return context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(new SkippedTestNodeStateProperty(reason))
        ));
    }

    public Task Cancelled(TestContext testContext, Exception exception)
    {
        return context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: _sessionSessionUid,
            testNode: testContext.ToTestNode()
                .WithProperty(new CancelledTestNodeStateProperty(exception))
        ));
    }

    public Task Artifact(Artifact artifact)
    {
        return context.MessageBus.PublishAsync(this,
            new SessionFileArtifact(
                context.Request.Session.SessionUid,
                artifact.File,
                artifact.DisplayName,
                artifact.Description
            )
        );
    }


    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;
    public string Version => extension.Version;
    public string DisplayName => extension.DisplayName;
    public string Description => extension.Description;
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage), typeof(SessionFileArtifact), typeof(FileArtifact)];
}