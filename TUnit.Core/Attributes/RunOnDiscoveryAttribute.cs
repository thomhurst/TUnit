using System.Threading.Tasks;
using TUnit.Core.Contexts;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class RunOnDiscoveryAttribute : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestDiscovered(TestDiscoveryContext context)
    {
        context.SetRunOnDiscovery(true);
        return default;
    }
}