using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace TUnit.Engine.Helpers;

internal static class ExceptionsHelper
{
    [StackTraceHidden]
    public static void ThrowIfAny(IReadOnlyList<Exception> exceptions)
    {
        if (exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
        }

        if (exceptions.Count > 1)
        {
            throw new AggregateException(exceptions);
        }
    }
}
