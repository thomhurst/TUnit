using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

internal class AssertionsTracker
{
    private static readonly AsyncLocal<List<BaseAssertCondition>> AsyncLocal = new();

    private static readonly object CurrentLock = new();
    public static List<BaseAssertCondition> Current
    {
        get
        {
            lock (CurrentLock)
            {
                return AsyncLocal.Value ??= [];
            }
        }
    }
}