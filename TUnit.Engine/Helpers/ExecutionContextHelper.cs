﻿using System.Runtime.CompilerServices;

namespace TUnit.Engine.Helpers;

internal static class ExecutionContextHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RestoreContexts(ExecutionContext[] executionContexts)
    {
#if NET
        foreach (var executionContext in executionContexts)
        {
            ExecutionContext.Restore(executionContext);
        }
#endif
    }
}
