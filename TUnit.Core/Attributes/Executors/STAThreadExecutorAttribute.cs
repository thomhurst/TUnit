using System.Runtime.Versioning;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[SupportedOSPlatform("windows")]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public class STAThreadExecutorAttribute : TUnitAttribute, ITestRegisteredEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        var executor = new STAThreadExecutor();
        context.SetTestExecutor(executor);
        return default;
    }
}
