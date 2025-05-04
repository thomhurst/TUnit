using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Services;

[SuppressMessage("Trimming", "IL2070:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
internal class ReflectionHooksCollector(string sessionId) : HooksCollectorBase(sessionId)
{
    public override void CollectDiscoveryHooks()
    {
        // var types = ReflectionTypeScanner.GetTypes();
        //
        // types
        //     .SelectMany(x => x.GetMethods())
        //     .Where(x => HasHookType(x, HookType.TestDiscovery));
    }

    public override void CollectionTestSessionHooks()
    {
        // TODO: Implement this method
    }

    public override void CollectHooks()
    {
        // TODO: Implement this method
    }

    private bool HasHookType(MethodInfo methodInfo, HookType hookType)
    {
        return methodInfo.GetCustomAttributes()
            .Any(x => IsHookType(x, hookType));
    }

    private static bool IsHookType(Attribute x, HookType hookType)
    {
        return x switch
        {
            BeforeAttribute beforeAttribute when beforeAttribute.HookType == hookType => true,
            BeforeEveryAttribute beforeEveryAttribute when beforeEveryAttribute.HookType == hookType => true,
            AfterAttribute afterAttribute when afterAttribute.HookType == hookType => true,
            _ => x is AfterEveryAttribute afterEveryAttribute && afterEveryAttribute.HookType == hookType
        };
    }
}