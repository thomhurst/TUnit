using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;

namespace TUnit.Engine.Services;

public class FailedInitializationTestPublisher(IExtension extension) : IDataProducer
{
    public async ValueTask NotifyFailedTests(ExecuteRequestContext context,
        IEnumerable<FailedInitializationTest> failedToInitializeTests, bool isDiscovered)
    {
        foreach (var failedToInitializeTest in failedToInitializeTests)
        {
            var testClass = failedToInitializeTest.TestClass!;
            var failedNode = new TestNode
            {
                Uid = failedToInitializeTest.TestId,
                DisplayName = failedToInitializeTest.TestName,
                Properties = new PropertyBag(
                    isDiscovered ? DiscoveredTestNodeStateProperty.CachedInstance : new ErrorTestNodeStateProperty(failedToInitializeTest.Exception, "Test failed to initialize"),
                    new TestFileLocationProperty(failedToInitializeTest.TestFilePath,
                        new LinePositionSpan(new LinePosition(failedToInitializeTest.TestLineNumber, 0),
                            new LinePosition(failedToInitializeTest.TestLineNumber, 0))
                    ),
                    new TestMethodIdentifierProperty(
                        AssemblyFullName: testClass.Assembly.FullName!,
                        Namespace: testClass.Namespace!,
                        TypeName: testClass.Name,
                        MethodName: failedToInitializeTest.TestName,
                        ParameterTypeFullNames: failedToInitializeTest.ParameterTypeFullNames.Select(x => x.FullName!).ToArray(),
                        ReturnTypeFullName: failedToInitializeTest.ReturnType.FullName!
                    ),
                    // TRX Reports
                    new TrxExceptionProperty(failedToInitializeTest.Exception.Message, failedToInitializeTest.Exception.StackTrace)
                )
            };
                        
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                sessionUid: context.Request.Session.SessionUid,
                testNode: failedNode)
            );
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
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];
}