using TUnit.Core.Interfaces;

// [assembly: ClassDisplayName]
namespace TUnit.TestProject.Attributes;

public class ClassDisplayNameAttribute : Attribute, ITestDiscoveryEventReceiver
{
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.SetDisplayName($"{context.Metadata.TestDetails.MethodMetadata.Class.Name}.{context. Metadata.DisplayName}");
        return default(ValueTask);
    }

    public int Order => 0;
}
