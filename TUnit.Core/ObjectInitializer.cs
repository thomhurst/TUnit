using System.Diagnostics.CodeAnalysis;
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
        await InitializeProperties(obj);

        if (obj is not IAsyncInitializer asyncInitializer)
        {
            return;
        }

        await GetInitializationTask(obj, asyncInitializer);
    }

    private static Task GetInitializationTask(object obj, IAsyncInitializer asyncInitializer)
    {
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

    [UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    private static async Task InitializeProperties(object? obj)
    {
        if (obj is null)
        {
            return;
        }

        if (!Sources.Properties.TryGetValue(obj.GetType(), out var properties))
        {
#if NET
            if (RuntimeFeature.IsDynamicCodeSupported)
#endif
            {
                properties = obj.GetType().GetProperties();
            }
        }

        foreach (var property in properties?.Where(p => p.PropertyType.IsAssignableTo(typeof(IAsyncInitializer))) ?? [])
        {
            if (property.GetValue(obj) is {} propertyValue)
            {
                await InitializeAsync(propertyValue);
            }
        }
    }
}
