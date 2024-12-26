using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class ParallelGroupAttribute(string group) : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public int Order => 0;

    public string Group { get; } = group;

    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.SetParallelConstraint(new ParallelGroupConstraint(Group));
    }
}