namespace TUnit.Core.Tracking;

/// <summary>
/// Provides unified object tracking for all test objects with simple Track/Untrack interface.
/// </summary>
public static class ObjectTrackerProvider
{
    /// <summary>
    /// Single unified tracker for all objects with full lifecycle management.
    /// Auto-disposal is always enabled when reference count reaches zero.
    /// </summary>
    private static readonly UnifiedObjectTracker Tracker = new(
        enableRecursiveTracking: true);

    /// <summary>
    /// Tracks an object for lifecycle management.
    /// </summary>
    /// <param name="obj">The object to track</param>
    /// <returns>The same object instance</returns>
    public static object? Track(object? obj)
    {
        return Tracker.TrackObject(obj);
    }

    /// <summary>
    /// Untracks and releases an object, disposing it if necessary.
    /// </summary>
    /// <param name="obj">The object to untrack and release</param>
    public static async Task Untrack(object? obj)
    {
        await Tracker.ReleaseObject(obj);
    }

    /// <summary>
    /// Gets the reference counter for a tracked object.
    /// </summary>
    /// <param name="obj">The object to check</param>
    /// <param name="counter">The reference counter if found</param>
    /// <returns>True if the object is tracked</returns>
    public static bool TryGetReference(object? obj, out TUnit.Core.Helpers.Counter? counter)
    {
        return Tracker.TryGetReference(obj, out counter);
    }

    /// <summary>
    /// Removes an object from tracking without disposal.
    /// </summary>
    /// <param name="obj">The object to remove</param>
    /// <returns>True if the object was removed</returns>
    public static bool Remove(object? obj)
    {
        return Tracker.RemoveObject(obj);
    }
}
