using TUnit.Core;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;

namespace TUnit.Engine.Services;

internal class TestInvoker
{
    private readonly Disposer _disposer;

    public TestInvoker(Disposer disposer)
    {
        _disposer = disposer;
    }
    
    public async Task Invoke(DiscoveredTest discoveredTest, CancellationToken cancellationToken)
    {
        await TestHookOrchestrator.ExecuteBeforeHooks(discoveredTest.TestContext.TestDetails.ClassInstance!, discoveredTest);
            
        await Timings.Record("Main Test Body", discoveredTest.TestContext, () => discoveredTest.ExecuteTest(cancellationToken));
    }
}