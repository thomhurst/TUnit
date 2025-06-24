using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

[assembly: ClassDisplayName]
namespace TUnit.TestProject.Attributes;

public class ClassDisplayNameAttribute : Attribute, ITestDiscoveryEventReceiver
{
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.SetDisplayName($"{context.TestDetails.TestClass.Name}.{context.TestContext.GetTestDisplayName()}");
        return default;
    }

    public int Order => 0;
}