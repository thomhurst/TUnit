using TUnit.Core.Interfaces;

namespace TUnit.Core.Tracking;

/// <summary>
/// Tracks objects for disposal with per-context idempotency.
/// Each test context tracks objects independently, but only once per context.
/// </summary>
internal static class ObjectLifecycleTracker
{
    /// <summary>
    /// Tracks an object for disposal exactly once per test context.
    /// The same object can be tracked in multiple test contexts for proper reference counting.
    /// </summary>
    /// <param name="events">Test context events for OnTestFinalized registration</param>
    /// <param name="obj">Object to track for disposal</param>
    /// <returns>True if this is the first tracking call for this object in this context</returns>
    public static bool TrackObjectForDisposal(TestContextEvents events, object? obj)
    {
        if (obj == null || events == null)
        {
            return false;
        }

        // Use per-context tracking set to ensure idempotency within this test context
        lock (events.TrackedObjects)
        {
            // Attempt to add - returns false if already tracked in this context
            if (!events.TrackedObjects.Add(obj))
            {
                return false; // Already tracked in this context, no-op
            }
        }

        // First time tracking this object in this context - delegate to ObjectTracker
        ObjectTracker.TrackObject(events, obj);
        return true;
    }
}