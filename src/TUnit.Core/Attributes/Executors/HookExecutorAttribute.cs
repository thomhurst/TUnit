using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public class HookExecutorAttribute : TUnitAttribute, IHookRegisteredEventReceiver, IScopedAttribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private readonly Type _hookExecutorType;

    private IHookExecutor? _executor;

    public HookExecutorAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        _hookExecutorType = type;
    }

    public Type HookExecutorType => _hookExecutorType;

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public Type ScopeType => typeof(IHookExecutor);

    /// <inheritdoc />
    public ValueTask OnHookRegistered(HookRegisteredContext context)
    {
        context.HookExecutor = _executor ??= (IHookExecutor)Activator.CreateInstance(_hookExecutorType)!;
        return default(ValueTask);
    }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class HookExecutorAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() : HookExecutorAttribute(typeof(T)) where T : IHookExecutor, new();
