using TUnit.Core;

namespace TUnit.Engine.Helpers;

internal static class ExecutionContextHelper
{
    public static void RestoreContexts(Context context) => RestoreContexts(context.ExecutionContexts);
    
    public static void RestoreContexts(List<ExecutionContext> executionContexts)
    {
#if NET
        foreach (var executionContext in executionContexts)
        {
            ExecutionContext.Restore(executionContext);
        }
#endif
    }
}