using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

internal static class HookExecutorProvider
{
    public static IHookExecutor GetHookExecutor(InstanceHookMethod instanceMethod, DiscoveredTest discoveredTest)
    {
        return discoveredTest.HookExecutor ?? instanceMethod.HookExecutor;
    }
    
    public static IHookExecutor GetHookExecutor<TClassType>(StaticHookMethod<TClassType> staticHookMethod, DiscoveredTest discoveredTest)
    {
        return discoveredTest.HookExecutor ?? staticHookMethod.HookExecutor;
    }
}