using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

[assembly: ClassDisplayName]
namespace TUnit.TestProject.Attributes;

public class ClassDisplayNameAttribute : Attribute, ITestDiscoveryEventReceiver
{
    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.SetDisplayName($"{discoveredTestContext.TestDetails.TestClass.Name}.{discoveredTestContext.TestContext.GetTestDisplayName()}");
    }

    public int Order { get; } = 0;
}