using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class CategoryAttribute(string category) : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public string Category { get; } = category;
    
    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.TestDetails.MutableCategories.Add(Category);
    }
}