using TUnit.Core.Interfaces;

namespace TUnit.Core.Tracking;

/// <summary>
/// Ensures exactly-once tracking of objects for disposal through idempotency.
/// Prevents duplicate tracking that causes premature disposal due to incorrect reference counting.
/// Tracking is scoped per TestContextEvents to ensure proper isolation between test contexts.
/// </summary>
internal static class ObjectLifecycleTracker
{
    /// <summary>
    /// Tracks an object for disposal exactly once per test context.
    /// Subsequent calls with the same object reference in the same test context are no-ops.
    /// </summary>
    /// <param name="events">Test context events for OnTestFinalized registration</param>
    /// <param name="obj">Object to track for disposal</param>
    /// <returns>True if this is the first tracking call for this object in this context, false if already tracked</returns>
    public static bool TrackObjectForDisposal(TestContextEvents events, object? obj)
    {
        if (obj == null || events == null)
        {
            return false;
        }

        // Use the per-context tracking set to ensure idempotency within this test context
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