using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

[MyDynamicallyAddedPropertyEvent]
public class FilterByDynamicAddedPropertyTests
{
    [Test]
    public void Test1()
    {
    }
    
    public class MyDynamicallyAddedPropertyEvent : Attribute, ITestDiscoveryEvent
    {
        public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
        {
            discoveredTestContext.AddProperty("MyKey", "MyDynamicallyAddedValue");
        }
    }
}