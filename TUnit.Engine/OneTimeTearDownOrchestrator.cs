using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace TUnit.Engine;

public static class OneTimeTearDownOrchestrator
{
    private static readonly ConcurrentDictionary<Type, int> RemainingTests = new();

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterTest(Type testClassType)
    {
        var count = RemainingTests.GetOrAdd(testClassType, 0);

        RemainingTests[testClassType] = count + 1;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static int NotifyCompletedTestAndGetRemainingTestsForType(Type testClassType)
    {
        var count = RemainingTests.GetOrAdd(testClassType, 0);

        var remainingTest = count - 1;
        
        RemainingTests[testClassType] = remainingTest;

        return remainingTest;
    }
}