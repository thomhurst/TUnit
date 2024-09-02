using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Hooks;

internal static class HookExecutorProvider
{
    public static IHookExecutor GetHookExecutor<TClassType>(InstanceHookMethod<TClassType> instanceMethod, DiscoveredTest discoveredTest)
    {
        return discoveredTest.HookExecutor ?? instanceMethod.HookExecutor;
    }
    
    public static IHookExecutor GetHookExecutor<TClassType>(StaticHookMethod<TClassType> staticHookMethod, DiscoveredTest discoveredTest)
    {
        return discoveredTest.HookExecutor ?? staticHookMethod.HookExecutor;
    }
}