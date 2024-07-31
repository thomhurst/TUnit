namespace TUnit.Core.Interfaces;

public interface IOnTestDiscoveryAttribute
{
    void OnTestDiscovery(DiscoveredTestContext discoveredTestContext);
}