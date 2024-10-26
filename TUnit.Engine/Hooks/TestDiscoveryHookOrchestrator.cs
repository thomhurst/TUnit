using TUnit.Core;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal class TestDiscoveryHookOrchestrator(HooksCollector hooksCollector, string? stringFilter)
{
    private BeforeTestDiscoveryContext? _beforeContext;
    private TestDiscoveryContext? _afterContext;

    public async Task ExecuteBeforeHooks()
    {
        var context = GetBeforeContext();
        
        BeforeTestDiscoveryContext.Current = context;

        foreach (var staticHookMethod in hooksCollector.BeforeTestDiscoveryHooks)
        {
            await staticHookMethod.Body(context, default);
        }
        
        BeforeTestDiscoveryContext.Current = null;
    }

    public async Task ExecuteAfterHooks(IEnumerable<DiscoveredTest> discoveredTests)
    {
        List<Exception> cleanUpExceptions = [];

        var context = GetAfterContext(discoveredTests);
        
        TestDiscoveryContext.Current = context;
        
        foreach (var staticHookMethod in hooksCollector.AfterTestDiscoveryHooks)
        {
            await RunHelpers.RunSafelyAsync(() => staticHookMethod.Body(context, default), cleanUpExceptions);
        }
        
        TestDiscoveryContext.Current = null;
        
        ExceptionsHelper.ThrowIfAny(cleanUpExceptions);
    }
    
    private BeforeTestDiscoveryContext GetBeforeContext()
    {
        return _beforeContext ??= new BeforeTestDiscoveryContext
        {
            TestFilter = stringFilter
        };
    }

    private TestDiscoveryContext GetAfterContext(IEnumerable<DiscoveredTest> discoveredTests)
    {
        return _afterContext ??= new TestDiscoveryContext(discoveredTests)
        {
            TestFilter = stringFilter
        };
    }
}