using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class NotInParallelAttribute : SingleTUnitAttribute, ITestDiscoveryEventReceiver
{
    public string[] ConstraintKeys { get; } = [];

    public int Order { get; init; }

    public NotInParallelAttribute()
    {
    }

    public NotInParallelAttribute(string constraintKey) : this([constraintKey])
    {
        if (constraintKey is null or { Length: < 1 })
        {
            throw new ArgumentException("No constraint key was provided");
        }
    }

    public NotInParallelAttribute(string[] constraintKeys)
    {
        if (constraintKeys.Length != constraintKeys.Distinct().Count())
        {
            throw new ArgumentException("Duplicate constraint keys are not allowed.");
        }
        
        ConstraintKeys = constraintKeys;
    }

    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.SetNotInParallelConstraints(ConstraintKeys, Order);
    }
}