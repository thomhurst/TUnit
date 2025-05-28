namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an interface for receivers that respond to test discovery events in the TUnit testing framework.
/// </summary>
/// <remarks>
/// Implementations of this interface receive notifications when tests are discovered during the test discovery phase.
/// This allows components to modify test properties, add categories, set display names, or perform other
/// operations that affect how tests are identified and presented before they are executed.
/// Test discovery event receivers are typically implemented by attributes that need to influence
/// test metadata during the discovery phase.
/// </remarks>
public interface ITestDiscoveryEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test is discovered by the test discovery process.
    /// </summary>
    /// <param name="discoveredTestContext">The context containing information about the discovered test and providing
    /// methods to modify the test's properties.</param>
    /// <remarks>
    /// This method allows implementations to inspect and modify test metadata during the discovery phase.
    /// Common modifications include setting custom display names, adding test categories, configuring
    /// parallelization constraints, and setting retry behavior.
    /// </remarks>
    void OnTestDiscovery(DiscoveredTestContext discoveredTestContext);
}