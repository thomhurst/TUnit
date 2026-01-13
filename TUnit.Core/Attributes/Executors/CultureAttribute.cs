using System.Globalization;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public class CultureAttribute(CultureInfo cultureInfo) : TUnitAttribute, ITestRegisteredEventReceiver, IScopedAttribute
{
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
        context.SetTestExecutor(new CultureExecutor(cultureInfo));
        return default(ValueTask);
    }
}
