using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TestExecutorAttribute<T> : TUnitAttribute, ITestRegisteredEventReceiver, IHookRegisteredEventReceiver, IScopedAttribute where T : ITestExecutor, new()
{
    // One executor instance per attribute — shared between test registration and hook
    // registration so the same object is wired up as both the test executor and the hook
    // executor (when T also implements IHookExecutor).
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
        // If the executor can also run hooks (e.g. GenericAbstractExecutor-based), reuse
        // the same instance so the whole test lifecycle runs in one execution context.
        if (executor is IHookExecutor hookExecutor)
        {
            context.SetHookExecutor(hookExecutor);
        }
        return default(ValueTask);
    }

    /// <inheritdoc />
    public ValueTask OnHookRegistered(HookRegisteredContext context)
    {
        // Only apply to class/assembly/session-level hooks if the executor can run them.
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

    // Cache: one executor instance + the hook-compatibility check, computed once per
    // attribute instance to avoid repeated Activator.CreateInstance calls and per-event
    // interface probes.
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
