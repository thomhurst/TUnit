using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class RunOnDiscoveryAttribute : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.SetRunOnDiscovery(true);
        return default;
    }
}
