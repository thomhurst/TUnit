using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class RunOnDiscoveryAttribute : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public int Order => 0;

    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.RunOnTestDiscovery = true;
    }
}