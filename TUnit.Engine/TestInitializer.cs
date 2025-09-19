using TUnit.Core;
using TUnit.Core.Initialization;
using TUnit.Engine.Extensions;
using TUnit.Engine.Services;

namespace TUnit.Engine;

internal class TestInitializer
{
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly TestObjectInitializer _testObjectInitializer;

    public TestInitializer(EventReceiverOrchestrator eventReceiverOrchestrator, TestObjectInitializer testObjectInitializer)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _testObjectInitializer = testObjectInitializer;
    }

    public async Task InitializeTest(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        // Use centralized TestObjectInitializer for all initialization
        await _testObjectInitializer.InitializeTestClassAsync(
            test.Context.TestDetails.ClassInstance,
            test.Context);

        // Initialize and register all eligible objects including event receivers
        await _eventReceiverOrchestrator.InitializeAllEligibleObjectsAsync(test.Context, cancellationToken).ConfigureAwait(false);
    }
}
