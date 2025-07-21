namespace TUnit.Core.ReferenceTracking;

public static class DataSourceReferenceExtensions
{
    /// <summary>
    /// Tracks all data source objects in the current test context.
    /// Call this at the beginning of your test if you want to manually manage references.
    /// </summary>
    public static void TrackDataSourceObjects(this TestContext context)
    {
        if (context?.TestDetails == null)
        {
            return;
        }

        var testDetails = context.TestDetails;

        foreach (var arg in testDetails.TestMethodArguments)
        {
            DataSourceReferenceTrackerProvider.TrackDataSourceObject(arg);
        }

        foreach (var arg in testDetails.TestClassArguments)
        {
            DataSourceReferenceTrackerProvider.TrackDataSourceObject(arg);
        }

        foreach (var kvp in testDetails.TestClassInjectedPropertyArguments)
        {
            DataSourceReferenceTrackerProvider.TrackDataSourceObject(kvp.Value);
        }
    }

    /// <summary>
    /// Releases all data source objects in the current test context.
    /// Call this at the end of your test or in cleanup hooks.
    /// </summary>
    /// <param name="context">The test context</param>
    /// <param name="disposeIfPossible">If true, dispose objects that implement IDisposable when their reference count reaches zero</param>
    public static async Task ReleaseDataSourceObjects(this TestContext context, bool disposeIfPossible = true)
    {
        var testDetails = context.TestDetails;

        foreach (var arg in testDetails.TestMethodArguments)
        {
            await DataSourceReferenceTrackerProvider.ReleaseDataSourceObject(arg);
        }

        foreach (var arg in testDetails.TestClassArguments)
        {
            await DataSourceReferenceTrackerProvider.ReleaseDataSourceObject(arg);
        }

        foreach (var kvp in testDetails.TestClassInjectedPropertyArguments)
        {
            await DataSourceReferenceTrackerProvider.ReleaseDataSourceObject(kvp.Value);
        }
    }

    public static DataSourceReferenceStats GetReferenceStats()
    {
        var tracker = DataSourceReferenceTrackerProvider.Instance;
        return new DataSourceReferenceStats
        {
            TrackedObjectCount = tracker.TrackedObjectCount,
            TotalActiveReferences = tracker.TotalActiveReferences,
            TrackedObjects = tracker.GetAllTrackedObjects()
                .Select(kvp => new TrackedObjectInfo
                {
                    ObjectType = kvp.Value.ObjectType,
                    ActiveCount = kvp.Value.ActiveCount,
                    FirstTracked = kvp.Value.FirstTracked,
                    LastAccessed = kvp.Value.LastAccessed
                })
                .ToList()
        };
    }
}

public class DataSourceReferenceStats
{
    public int TrackedObjectCount { get; init; }
    public int TotalActiveReferences { get; init; }
    public List<TrackedObjectInfo> TrackedObjects { get; init; } = new();
}

public class TrackedObjectInfo
{
    public Type ObjectType { get; init; } = null!;
    public int ActiveCount { get; init; }
    public DateTime FirstTracked { get; init; }
    public DateTime? LastAccessed { get; init; }
}
