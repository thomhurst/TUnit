namespace TUnit.Core.Tracking;

/// <summary>
/// Provides access to global UnifiedObjectTracker instances for different use cases.
/// </summary>
public static class ObjectTrackerProvider
{
    /// <summary>
    /// Tracker for data source objects with automatic disposal and recursive tracking.
    /// Replaces DataSourceReferenceTracker functionality.
    /// </summary>
    public static UnifiedObjectTracker DataSourceTracker { get; } = new(
        enableAutoDisposal: true, 
        enableRecursiveTracking: true);

    /// <summary>
    /// Simple tracker for test arguments without automatic disposal.
    /// Replaces ActiveObjectTracker functionality.
    /// </summary>
    public static UnifiedObjectTracker ArgumentTracker { get; } = new(
        enableAutoDisposal: false, 
        enableRecursiveTracking: false);

    /// <summary>
    /// Tracks objects created from data sources with full lifecycle management.
    /// Equivalent to DataSourceReferenceTrackerProvider.TrackDataSourceObject.
    /// </summary>
    /// <param name="obj">The object to track</param>
    /// <returns>The same object instance</returns>
    public static object? TrackDataSourceObject(object? obj)
    {
        return DataSourceTracker.TrackObject(obj);
    }

    /// <summary>
    /// Releases objects used in a test with full disposal handling.
    /// Equivalent to DataSourceReferenceTrackerProvider.ReleaseDataSourceObject.
    /// </summary>
    /// <param name="obj">The object to release</param>
    public static async Task ReleaseDataSourceObject(object? obj)
    {
        await DataSourceTracker.ReleaseObject(obj);
    }

    /// <summary>
    /// Increments usage count for test arguments.
    /// Equivalent to ActiveObjectTracker.IncrementUsage.
    /// </summary>
    /// <param name="obj">The object to track</param>
    public static void IncrementArgumentUsage(object? obj)
    {
        ArgumentTracker.TrackObject(obj);
    }

    /// <summary>
    /// Increments usage count for multiple test arguments.
    /// Equivalent to ActiveObjectTracker.IncrementUsage(IEnumerable).
    /// </summary>
    /// <param name="objects">The objects to track</param>
    public static void IncrementArgumentUsage(IEnumerable<object?> objects)
    {
        ArgumentTracker.TrackObjects(objects);
    }

    /// <summary>
    /// Tries to get the reference info for a test argument.
    /// Equivalent to ActiveObjectTracker.TryGetCounter.
    /// </summary>
    /// <param name="obj">The object to check</param>
    /// <param name="reference">The reference info if found</param>
    /// <returns>True if the object is tracked</returns>
    public static bool TryGetArgumentReference(object? obj, out UnifiedObjectTracker.TrackedReference? reference)
    {
        return ArgumentTracker.TryGetReference(obj, out reference);
    }

    /// <summary>
    /// Removes an argument object from tracking.
    /// Equivalent to ActiveObjectTracker.RemoveObject.
    /// </summary>
    /// <param name="obj">The object to remove</param>
    /// <returns>True if the object was removed</returns>
    public static bool RemoveArgumentObject(object? obj)
    {
        return ArgumentTracker.RemoveObject(obj);
    }
}