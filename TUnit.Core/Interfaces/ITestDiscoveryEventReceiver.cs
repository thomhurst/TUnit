namespace TUnit.Core.Interfaces;

public interface ITestDiscoveryEventReceiver : IEventReceiver
{
    void OnTestDiscovery(DiscoveredTestContext discoveredTestContext);
}