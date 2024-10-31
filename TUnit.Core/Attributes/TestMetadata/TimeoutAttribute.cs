using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class TimeoutAttribute(int timeoutInMilliseconds) : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public TimeSpan Timeout { get; } = TimeSpan.FromMilliseconds(timeoutInMilliseconds);
    
    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.TestDetails.Timeout = Timeout;
    }
}