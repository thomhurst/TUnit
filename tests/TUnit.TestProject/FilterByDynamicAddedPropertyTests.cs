using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

[MyDynamicallyAddedProperty]
public class FilterByDynamicAddedPropertyTests
{
    [Test]
    public async Task Test1()
    {
        // This class is only ever run through a property filter, which cannot be applied until every
        // test in the assembly has been built. Tests the filter excluded must not leak into the hook
        // contexts, or assembly/session hooks act on classes that are not part of the run.
        var sessionClasses = TestSessionContext.Current!.TestClasses.Select(c => c.ClassType).Distinct().ToArray();
        var assemblyClasses = TestContext.Current!.ClassContext.AssemblyContext.TestClasses.Select(c => c.ClassType).Distinct().ToArray();

        await Assert.That(sessionClasses).IsEquivalentTo([typeof(FilterByDynamicAddedPropertyTests)]);
        await Assert.That(assemblyClasses).IsEquivalentTo([typeof(FilterByDynamicAddedPropertyTests)]);
        await Assert.That(TestSessionContext.Current.AllTests).Count().IsEqualTo(1);
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
