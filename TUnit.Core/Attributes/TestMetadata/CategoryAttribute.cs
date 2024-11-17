using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class CategoryAttribute(string category) : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public string Category { get; } = category;
    
    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.AddCategory(Category);
    }
}