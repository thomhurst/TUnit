using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TestExecutorAttribute<T> : TUnitAttribute, ITestRegisteredEventReceiver, IHookRegisteredEventReceiver, IScopedAttribute where T : ITestExecutor, new()
{
    private T? _executor;
    private T Executor => _executor ??= new T();

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public Type ScopeType => typeof(ITestExecutor);

    /// <inheritdoc />
    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        var executor = Executor;
        context.SetTestExecutor(executor);
        if (executor is IHookExecutor hookExecutor)
        {
            context.SetHookExecutor(hookExecutor);
        }
        return default(ValueTask);
    }

    /// <inheritdoc />
    public ValueTask OnHookRegistered(HookRegisteredContext context)
    {
        if (Executor is IHookExecutor hookExecutor)
        {
            context.HookExecutor = hookExecutor;
        }
        return default(ValueTask);
    }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TestExecutorAttribute : TUnitAttribute, ITestRegisteredEventReceiver, IHookRegisteredEventReceiver, IScopedAttribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private readonly Type _type;

    private readonly bool _isHookExecutor;
    private ITestExecutor? _executor;

    public TestExecutorAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        _type = type;
        _isHookExecutor = typeof(IHookExecutor).IsAssignableFrom(type);
    }

    private ITestExecutor Executor => _executor ??= (ITestExecutor) Activator.CreateInstance(_type)!;

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public Type ScopeType => typeof(ITestExecutor);

    /// <inheritdoc />
    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        var executor = Executor;
        context.SetTestExecutor(executor);
        if (_isHookExecutor)
        {
            context.SetHookExecutor((IHookExecutor) executor);
        }
        return default(ValueTask);
    }

    /// <inheritdoc />
    public ValueTask OnHookRegistered(HookRegisteredContext context)
    {
        if (_isHookExecutor)
        {
            context.HookExecutor = (IHookExecutor) Executor;
        }
        return default(ValueTask);
    }
}
