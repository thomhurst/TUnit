using System.Collections.Concurrent;

namespace TUnit.Engine;

public static class OneTimeSetUpOrchestrator
{
    public static readonly ConcurrentDictionary<Type, Task> Tasks = new();
}