using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public static class ObjectInitializer
{
    private static readonly ConditionalWeakTable<object, Task> _initializationTasks = new();
    private static readonly Lock _lock = new();

    public static Task InitializeAsync(object? obj, CancellationToken cancellationToken = default)
    {
        if (obj is not IAsyncInitializer asyncInitializer)
        {
            return Task.CompletedTask;
        }

        lock (_lock)
        {
            if (_initializationTasks.TryGetValue(obj, out var task))
            {
                return task;
            }

            var initializationTask = asyncInitializer.InitializeAsync();

            _initializationTasks.Add(obj, initializationTask);

            return initializationTask;
        }
    }
}
