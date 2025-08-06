using TUnit.Core.Interfaces;

// [assembly: ClassDisplayName]
namespace TUnit.TestProject.Attributes;

public class ClassDisplayNameAttribute : Attribute, ITestDiscoveryEventReceiver
{
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.SetDisplayName($"{context.TestDetails.MethodMetadata.Class.Name}.{context.GetDisplayName()}");
        return default(ValueTask);
    }

    public int Order => 0;
}
