namespace TUnit.Core.Interfaces;

public interface ITestDiscoveryEvent
{
    void OnTestDiscovery(DiscoveredTestContext discoveredTestContext);
}