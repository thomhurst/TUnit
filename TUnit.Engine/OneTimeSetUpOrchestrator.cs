using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace TUnit.Engine;

public static class OneTimeSetUpOrchestrator
{
    private static readonly ConcurrentDictionary<Type, List<Task>> Tasks = new();

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static List<Task> GetOrAdd(Type type, Func<List<Task>> listDelegate)
    {
        return Tasks.GetOrAdd(type, _ => listDelegate());
    }
}