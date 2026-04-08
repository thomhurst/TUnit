using System.Globalization;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public class CultureAttribute(CultureInfo cultureInfo) : TUnitAttribute, ITestRegisteredEventReceiver, IHookRegisteredEventReceiver, IScopedAttribute
{
    // One executor per attribute instance — shared between test registration and hook
    // registration paths so we don't allocate twice when the attribute applies to both.
    // CultureExecutor is stateless per call (creates a fresh thread in ExecuteAsync), so
    // concurrent use across tests/hooks is safe.
    private CultureExecutor? _executor;
    private CultureExecutor Executor => _executor ??= new CultureExecutor(cultureInfo);

    public CultureAttribute(string cultureName) : this(CultureInfo.GetCultureInfo(cultureName))
    {
    }

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
        // on a thread with the same culture as the test body.
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
