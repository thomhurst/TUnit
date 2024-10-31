using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class PropertyAttribute(string name, string value) : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public string Name { get; } = name;
    public string Value { get; } = value;
    
    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.AddProperty(Name, Value);
    }
}