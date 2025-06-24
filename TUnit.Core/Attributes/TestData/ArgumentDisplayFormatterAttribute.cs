using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public abstract class ArgumentDisplayFormatterAttribute : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public virtual int Order => 0;

    public abstract ArgumentDisplayFormatter Formatter { get; }
    
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.AddArgumentDisplayFormatter(Formatter);
        return default;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class ArgumentDisplayFormatterAttribute<T> : ArgumentDisplayFormatterAttribute
    where T : ArgumentDisplayFormatter, new()
{
    public override ArgumentDisplayFormatter Formatter { get; } = new T();
}