namespace TUnit.Core.ReferenceTracking;

/// <summary>
/// Provides access to the global DataSourceReferenceTracker instance.
/// </summary>
public static class DataSourceReferenceTrackerProvider
{
    /// <summary>
    /// Gets the global DataSourceReferenceTracker instance.
    /// </summary>
    public static DataSourceReferenceTracker Instance { get; } = new();

    /// <summary>
    /// Tracks objects created from data sources.
    /// If the object is non-null and trackable, increments its reference count.
    /// </summary>
    /// <param name="obj">The object to track</param>
    /// <returns>The same object instance</returns>
    public static object? TrackDataSourceObject(object? obj)
    {
        if (obj == null)
        {
            return null;
        }

        // Skip tracking for primitive types and strings as they're immutable
        var type = obj.GetType();
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
        {
            return obj;
        }

        // Track the object itself
        Instance.TrackObject(obj);
        
        // If it's a collection, track all elements inside it
        if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
        {
            foreach (var item in enumerable)
            {
                TrackDataSourceObject(item); // Recursive tracking
            }
        }

        return obj;
    }

    /// <summary>
    /// Releases objects used in a test.
    /// If the object's reference count reaches zero, it can be disposed.
    /// </summary>
    /// <param name="obj">The object to release</param>
    public static async Task ReleaseDataSourceObject(object? obj)
    {
        if (obj == null)
        {
            return;
        }

        // Skip tracking for primitive types and strings
        var type = obj.GetType();
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
        {
            return;
        }

        // If it's a collection, release all elements inside it first
        if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
        {
            foreach (var item in enumerable)
            {
                await ReleaseDataSourceObject(item); // Recursive release
            }
        }
        
        // Then release the object itself
        await Instance.ReleaseObject(obj);
    }
}
