namespace TUnit.Core.Interfaces;

/// <summary>
/// Simplified interface for test discovery event receivers
/// </summary>
public interface ITestDiscoveryEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test is discovered
    /// </summary>
    ValueTask OnTestDiscovered(DiscoveredTestContext context);
}
