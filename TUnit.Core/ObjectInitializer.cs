using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public static class ObjectInitializer
{
    private static readonly ConditionalWeakTable<object, Task> _initializationTasks = new();
    private static readonly Lock _lock = new();

    internal static bool IsInitialized(object? obj)
    {
        if (obj is not IAsyncInitializer)
        {
            return false;
        }

        lock (_lock)
        {
            return _initializationTasks.TryGetValue(obj, out var task) && task.IsCompleted;
        }
    }

    public static async ValueTask InitializeAsync(object? obj, CancellationToken cancellationToken = default)
    {
        if (obj is IAsyncInitializer asyncInitializer)
        {
            await GetInitializationTask(obj, asyncInitializer, cancellationToken);
        }
    }

    private static async Task GetInitializationTask(object obj, IAsyncInitializer asyncInitializer, CancellationToken cancellationToken)
    {
        Task initializationTask;

        lock (_lock)
        {
            if (_initializationTasks.TryGetValue(obj, out var existingTask))
            {
                initializationTask = existingTask;
            }
            else
            {
                initializationTask = asyncInitializer.InitializeAsync();
                _initializationTasks.Add(obj, initializationTask);
            }
        }

        // Wait for initialization with cancellation support
        await initializationTask.WaitAsync(cancellationToken);
    }
}
