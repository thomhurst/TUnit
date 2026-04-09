using System.Runtime.Versioning;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[SupportedOSPlatform("windows")]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public class STAThreadExecutorAttribute : TUnitAttribute, ITestRegisteredEventReceiver, IHookRegisteredEventReceiver, IScopedAttribute
{
    private STAThreadExecutor? _executor;
    private STAThreadExecutor Executor => _executor ??= new STAThreadExecutor();

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public Type ScopeType => typeof(ITestExecutor);

    /// <inheritdoc />
    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        var executor = Executor;
        context.SetTestExecutor(executor);
        context.SetHookExecutor(executor);
        return default(ValueTask);
    }

    /// <inheritdoc />
    public ValueTask OnHookRegistered(HookRegisteredContext context)
    {
        context.HookExecutor = Executor;
        return default(ValueTask);
    }
}
