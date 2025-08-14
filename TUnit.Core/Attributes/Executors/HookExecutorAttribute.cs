using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

public class HookExecutorAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type) : TUnitAttribute, IHookRegisteredEventReceiver
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type HookExecutorType { get; } = type;
    
    public int Order => 0;

    [UnconditionalSuppressMessage("Trimming", 
        "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to target method",
        Justification = "HookExecutorType is annotated with required DynamicallyAccessedMembers")]
    public ValueTask OnHookRegistered(HookRegisteredContext context)
    {
        context.SetHookExecutor((IHookExecutor)Activator.CreateInstance(HookExecutorType)!);
        return default(ValueTask);
    }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class HookExecutorAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>() : HookExecutorAttribute(typeof(T)) where T : IHookExecutor, new();
