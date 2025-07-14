using TUnit.Core.Interfaces;

namespace TUnit.Engine.Tests.Attributes;

public class SetDisplayNameWithClassAttribute : Attribute, ITestDiscoveryEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.SetDisplayName(
            $"{context.TestDetails.ClassMetadata.Name}.{context.GetTestDisplayName()}");
        return default(ValueTask);
    }
}
