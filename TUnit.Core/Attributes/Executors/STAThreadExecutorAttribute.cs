using System.Runtime.Versioning;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[SupportedOSPlatform("windows")]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public class STAThreadExecutorAttribute : TUnitAttribute, ITestRegisteredEventReceiver, IHookRegisteredEventReceiver, IScopedAttribute
{
    // One executor per attribute instance — shared between test registration and hook
    // registration paths. STAThreadExecutor is stateless per call (creates a fresh STA
    // thread on ExecuteAsync), so concurrent use is safe.
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
        // Also wire up the executor for test-level hooks (Before/After(Test)) so they run
        // on the same STA thread as the test body.
        context.SetHookExecutor(executor);
        return default(ValueTask);
    }

    /// <inheritdoc />
    public ValueTask OnHookRegistered(HookRegisteredContext context)
    {
        // Applies to class/assembly/session-level hooks — those don't flow through
        // TestContext.CustomHookExecutor, so we set the hook's own executor instead.
        context.HookExecutor = Executor;
        return default(ValueTask);
    }
}
