using TUnit.Core;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal class TestSessionHookOrchestrator(HooksCollector hooksCollector, AssemblyHookOrchestrator assemblyHookOrchestrator, string? stringFilter)
{
    private TestSessionContext? _context;
    
    public async Task ExecuteBeforeHooks()
    {
        var context = GetContext();

        foreach (var staticHookMethod in hooksCollector.BeforeTestSessionHooks)
        {
            await staticHookMethod.Body(context, default);
        }
    }

    public async Task ExecuteAfterHooks()
    {
        List<Exception> cleanUpExceptions = [];
        
        var context = GetContext();
        
        foreach (var staticHookMethod in hooksCollector.AfterTestSessionHooks)
        {
            await RunHelpers.RunSafelyAsync(() => staticHookMethod.Body(context, default), cleanUpExceptions);
        }
        
        ExceptionsHelper.ThrowIfAny(cleanUpExceptions);
    }
    
    private TestSessionContext GetContext()
    {
        return _context ??= new TestSessionContext(assemblyHookOrchestrator.GetAllAssemblyHookContexts())
        {
            TestFilter = stringFilter
        };
    }
}