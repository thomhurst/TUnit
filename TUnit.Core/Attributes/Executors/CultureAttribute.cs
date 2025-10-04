using System.Globalization;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public class CultureAttribute(CultureInfo cultureInfo) : TUnitAttribute, ITestRegisteredEventReceiver
{
    public CultureAttribute(string cultureName) : this(CultureInfo.GetCultureInfo(cultureName))
    {
    }

    public int Order => 0;

#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
#endif
    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetTestExecutor(new CultureExecutor(cultureInfo));
        return default(ValueTask);
    }
}
