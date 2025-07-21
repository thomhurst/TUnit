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
            ObjectTrackerProvider.Track(arg);
        }

        foreach (var arg in testDetails.TestClassArguments)
        {
            ObjectTrackerProvider.Track(arg);
        }

        foreach (var kvp in testDetails.TestClassInjectedPropertyArguments)
        {
            ObjectTrackerProvider.Track(kvp.Value);
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
            await ObjectTrackerProvider.Untrack(arg);
        }

        foreach (var arg in testDetails.TestClassArguments)
        {
            await ObjectTrackerProvider.Untrack(arg);
        }

        foreach (var kvp in testDetails.TestClassInjectedPropertyArguments)
        {
            await ObjectTrackerProvider.Untrack(kvp.Value);
        }
    }

}
