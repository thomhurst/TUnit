using System.Diagnostics;

namespace TUnit.Engine.Helpers;

internal static class ExceptionsHelper
{
    [StackTraceHidden]
    public static void ThrowIfAny(IReadOnlyList<Exception> exceptions)
    {
        if (exceptions.Count == 1)
        {
            throw exceptions[0];
        }

        if (exceptions.Count > 1)
        {
            throw new AggregateException(exceptions);
        }
    }
}