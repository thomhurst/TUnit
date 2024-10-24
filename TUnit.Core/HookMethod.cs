namespace TUnit.Core;

public class HookMethod
{
    public StaticHookMethod? StaticHookMethod { get; }
    public InstanceHookMethod? InstanceHookMethod { get; }

    public HookMethod(InstanceHookMethod instanceHookMethod)
    {
        InstanceHookMethod = instanceHookMethod;
    }

    public HookMethod(StaticHookMethod staticHookMethod)
    {
        StaticHookMethod = staticHookMethod;
    }

    public static implicit operator HookMethod(InstanceHookMethod instanceHookMethod) => new(instanceHookMethod);
    public static implicit operator HookMethod(StaticHookMethod staticHookMethod) => new(staticHookMethod);
}