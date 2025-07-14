using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

//[assembly: ClassDisplayName]
namespace TUnit.TestProject.Attributes;

public class ClassDisplayNameAttribute : Attribute, ITestDiscoveryEventReceiver
{
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.SetDisplayName($"{context.TestDetails.ClassMetadata.Name}.{context.GetTestDisplayName()}");
        return default(ValueTask);
    }

    public int Order => 0;
}
