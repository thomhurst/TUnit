using TUnit.Core.Tracking;

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
            ObjectTrackerProvider.TrackDataSourceObject(arg);
        }

        foreach (var arg in testDetails.TestClassArguments)
        {
            ObjectTrackerProvider.TrackDataSourceObject(arg);
        }

        foreach (var kvp in testDetails.TestClassInjectedPropertyArguments)
        {
            ObjectTrackerProvider.TrackDataSourceObject(kvp.Value);
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
            await ObjectTrackerProvider.ReleaseDataSourceObject(arg);
        }

        foreach (var arg in testDetails.TestClassArguments)
        {
            await ObjectTrackerProvider.ReleaseDataSourceObject(arg);
        }

        foreach (var kvp in testDetails.TestClassInjectedPropertyArguments)
        {
            await ObjectTrackerProvider.ReleaseDataSourceObject(kvp.Value);
        }
    }

}
