using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class TestSessionHookOrchestrator(HookMessagePublisher hookMessagePublisher)
{
    public static async Task ExecuteBeforeHooks(TestSessionContext context)
    {
        foreach (var setUp in TestDictionary.BeforeTestSession.OrderBy(x => x.HookMethod.Order))
        {
            try
            {
                TestSessionContext.Current = context;

                await setUp.Action(context);
            }
            finally
            {
                TestSessionContext.Current = null;
            }
        }
    }

    public static async Task ExecuteAfterHooks(TestSessionContext context)
    {
        List<Exception> exceptions = []; 

        foreach (var cleanUp in TestDictionary.AfterTestSession.OrderBy(x => x.HookMethod.Order))
        {
            try
            {
                TestSessionContext.Current = context;
                
                await RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), exceptions);
            }
            finally
            {
                TestSessionContext.Current = null;
            }
        }
        
        ExceptionsHelper.ThrowIfAny(exceptions);
    }
}