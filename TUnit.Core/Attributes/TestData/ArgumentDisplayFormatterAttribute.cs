using TUnit.Core.Interfaces;

namespace TUnit.Core;

public abstract class ArgumentDisplayFormatterAttribute : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public abstract ArgumentDisplayFormatter Formatter { get; }
    
    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.AddArgumentDisplayFormatter(Formatter);
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class ArgumentDisplayFormatterAttribute<T> : ArgumentDisplayFormatterAttribute
    where T : ArgumentDisplayFormatter, new()
{
    public override ArgumentDisplayFormatter Formatter { get; } = new T();
}