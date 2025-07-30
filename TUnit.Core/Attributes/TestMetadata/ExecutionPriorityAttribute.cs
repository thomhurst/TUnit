using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class ExecutionPriorityAttribute : SingleTUnitAttribute, ITestDiscoveryEventReceiver, IScopedAttribute<ExecutionPriorityAttribute>
{
    public Priority Priority { get; }
    public int Order => 0;

    public ExecutionPriorityAttribute(Priority priority = Priority.Normal)
    {
        Priority = priority;
    }

    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.SetPriority(Priority);
        return default(ValueTask);
    }
}
