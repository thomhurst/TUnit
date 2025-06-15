using System.Runtime.ExceptionServices;

namespace TUnit.Core.Helpers;

/// <summary>
/// Helper class to properly handle async operations in synchronous contexts,
/// particularly during test discovery which must be synchronous in Microsoft.Testing.Platform
/// </summary>
internal static class AsyncToSyncHelper
{
    /// <summary>
    /// Executes an async operation synchronously, properly handling exceptions and context
    /// </summary>
    public static TResult RunSync<TResult>(Func<Task<TResult>> asyncOperation)
    {
        // Use a dedicated thread to avoid potential deadlocks
        var task = Task.Run(asyncOperation);
        
        try
        {
            return task.GetAwaiter().GetResult();
        }
        catch (AggregateException ae)
        {
            // Unwrap and rethrow the first exception with original stack trace
            if (ae.InnerExceptions.Count == 1)
            {
                ExceptionDispatchInfo.Capture(ae.InnerException!).Throw();
            }
            throw;
        }
    }
    
    /// <summary>
    /// Executes an async operation synchronously without a return value
    /// </summary>
    public static void RunSync(Func<Task> asyncOperation)
    {
        // Use a dedicated thread to avoid potential deadlocks
        var task = Task.Run(asyncOperation);
        
        try
        {
            task.GetAwaiter().GetResult();
        }
        catch (AggregateException ae)
        {
            // Unwrap and rethrow the first exception with original stack trace
            if (ae.InnerExceptions.Count == 1)
            {
                ExceptionDispatchInfo.Capture(ae.InnerException!).Throw();
            }
            throw;
        }
    }
    
    /// <summary>
    /// Enumerates an async enumerable synchronously
    /// </summary>
    public static IEnumerable<T> EnumerateSync<T>(IAsyncEnumerable<T> asyncEnumerable, CancellationToken cancellationToken = default)
    {
        var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                var hasNext = RunSync(() => enumerator.MoveNextAsync().AsTask());
                if (!hasNext)
                {
                    break;
                }
                
                yield return enumerator.Current;
            }
        }
        finally
        {
            RunSync(() => enumerator.DisposeAsync().AsTask());
        }
    }
    
    /// <summary>
    /// Unwraps a Task or ValueTask to get its result synchronously
    /// </summary>
    public static object? UnwrapTaskResult(object? taskLikeObject)
    {
        if (taskLikeObject is null)
        {
            return null;
        }
        
        var type = taskLikeObject.GetType();
        
        // Handle Task<T>
        if (typeof(Task).IsAssignableFrom(type))
        {
            var task = (Task)taskLikeObject;
            RunSync(() => task);
            
            if (type.IsGenericType)
            {
                var resultProperty = type.GetProperty("Result");
                return resultProperty?.GetValue(task);
            }
            
            return Array.Empty<object>();
        }
        
        // Handle ValueTask<T> and ValueTask
        if (type.Name.StartsWith("ValueTask"))
        {
            // Convert ValueTask to Task for easier handling
            var asTaskMethod = type.GetMethod("AsTask");
            var convertedTask = (Task)asTaskMethod!.Invoke(taskLikeObject, null)!;
            
            return UnwrapTaskResult(convertedTask);
        }
        
        return taskLikeObject;
    }
}