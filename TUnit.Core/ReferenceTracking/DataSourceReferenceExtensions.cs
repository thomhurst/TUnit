using TUnit.Core.Tracking;

namespace TUnit.Core.ReferenceTracking;

public static class DataSourceReferenceExtensions
{
    /// <summary>
    /// Tracks all data source objects in the current test context.
    /// Call this at the beginning of your test if you want to manually manage references.
    /// </summary>
    public static void TrackMethodArguments(this TestContext context)
    {
        foreach (var arg in context.TestDetails.TestMethodArguments)
        {
            UnifiedObjectTracker.TrackObject(context.Events, arg);
        }
    }

    public static void TrackClassArguments(this TestContext context)
    {
        foreach (var arg in context.TestDetails.ClassMetadataArguments)
        {
            UnifiedObjectTracker.TrackObject(context.Events, arg);
        }
    }

    public static void TrackPropertyArguments(this TestContext context)
    {
        foreach (var kvp in context.TestDetails.TestClassInjectedPropertyArguments)
        {
            UnifiedObjectTracker.TrackObject(context.Events, kvp.Value);
        }
    }
}
