using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class TestDiscoveryHookOrchestrator(HookMessagePublisher hookMessagePublisher)
{
    public static async Task ExecuteBeforeHooks(BeforeTestDiscoveryContext context)
    {
        foreach (var setUp in TestDictionary.BeforeTestDiscovery.OrderBy(x => x.HookMethod.Order))
        {
            BeforeTestDiscoveryContext.Current = context;

            try
            {
                await setUp.Action(context);
            }
            finally
            {
                BeforeTestDiscoveryContext.Current = null;
            }
        }
    }

    public static async Task ExecuteAfterHooks(TestDiscoveryContext context)
    {
        List<Exception> exceptions = []; 
        
        foreach (var cleanUp in TestDictionary.AfterTestDiscovery.OrderBy(x => x.HookMethod.Order))
        {
            try
            {
                TestDiscoveryContext.Current = context;

                await RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), exceptions);
            }
            finally
            {
                TestDiscoveryContext.Current = null;
            }
        }
        
        ExceptionsHelper.ThrowIfAny(exceptions);
    }
}