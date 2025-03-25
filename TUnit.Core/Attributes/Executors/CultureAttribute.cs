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

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.DiscoveredTest.TestExecutor = new CultureExecutor(cultureInfo);

        return default;
    }
}