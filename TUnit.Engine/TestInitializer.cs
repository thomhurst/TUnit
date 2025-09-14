using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Services;

namespace TUnit.Engine;

internal class TestInitializer
{
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;

    public TestInitializer(EventReceiverOrchestrator eventReceiverOrchestrator)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
    }

    public async Task InitializeTest(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        await PropertyInjectionService.InjectPropertiesIntoObjectAsync(
            test.Context.TestDetails.ClassInstance,
            test.Context.ObjectBag,
            test.Context.TestDetails.MethodMetadata,
            test.Context.Events);

        // Initialize and register all eligible objects including event receivers
        await _eventReceiverOrchestrator.InitializeAllEligibleObjectsAsync(test.Context, cancellationToken).ConfigureAwait(false);
    }
}
