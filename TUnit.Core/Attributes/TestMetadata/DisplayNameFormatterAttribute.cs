using TUnit.Core.Interfaces;

#pragma warning disable CS9113 // Parameter is unread - Used for source generator

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, Inherited = false)]
public abstract class DisplayNameFormatterAttribute : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public int Order => 0;

    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        var displayName = FormatDisplayName(discoveredTestContext.TestContext);

        discoveredTestContext.SetDisplayName(displayName);
    }

    protected abstract string FormatDisplayName(TestContext testContext);
}
