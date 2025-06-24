using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class RunOnDiscoveryAttribute : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestDiscovered(TestContext testContext)
    {
        testContext.RunOnTestDiscovery = true;
        return default;
    }
}