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
        public ValueTask OnTestDiscovered(DiscoveredTestContext context)
        {
            context.AddProperty("MyKey", "MyDynamicallyAddedValue");
            return default(ValueTask);
        }

        public int Order => 0;
    }
}
