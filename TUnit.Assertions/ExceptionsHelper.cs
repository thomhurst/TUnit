using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace TUnit.Assertions;

internal static class ExceptionsHelper
{
    [StackTraceHidden]
    public static void ThrowIfAny(IReadOnlyList<Exception> exceptions)
    {
        if (exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Throw(exceptions[0]);
        }

        if (exceptions.Count > 1)
        {
            throw new AggregateException(exceptions);
        }
    }
}