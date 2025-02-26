using System.Runtime.CompilerServices;
using TUnit.Core;

namespace TUnit.Engine.Helpers;

internal static class ExecutionContextHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RestoreContext(Context context) => RestoreContext(context.ExecutionContext);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RestoreContext(ExecutionContext? executionContext)
    {
#if NET
        if (executionContext != null)
        {
            ExecutionContext.Restore(executionContext);
        }
#endif
    }
}