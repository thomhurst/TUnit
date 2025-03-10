using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Tests.Attributes;

public class SetDisplayNameWithClassAttribute : Attribute, ITestDiscoveryEventReceiver
{
    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.SetDisplayName(
            $"{discoveredTestContext.TestDetails.TestClass.Name}.{discoveredTestContext.TestContext.GetTestDisplayName()}");
    }
}