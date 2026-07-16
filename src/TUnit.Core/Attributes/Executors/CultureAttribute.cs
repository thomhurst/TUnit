using System.Globalization;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public class CultureAttribute(CultureInfo cultureInfo) : TUnitAttribute, ITestRegisteredEventReceiver, IHookRegisteredEventReceiver, IScopedAttribute
{
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
