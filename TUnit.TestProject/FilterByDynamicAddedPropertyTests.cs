using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

[MyDynamicallyAddedProperty]
public class FilterByDynamicAddedPropertyTests
{
    [Test]
    public void Test1()
    {
    }
    
    public class MyDynamicallyAddedPropertyAttribute : Attribute, ITestDiscoveryEventReceiver
    {
        public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
        {
            discoveredTestContext.AddProperty("MyKey", "MyDynamicallyAddedValue");
        }
    }
}