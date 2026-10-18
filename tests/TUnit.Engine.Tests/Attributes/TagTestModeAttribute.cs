using TUnit.Core.Interfaces;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests.Attributes;

// Tags each test with the TestMode its class runs in, so CI can run one mode with
// --treenode-filter "/*/*/*/*[EngineTestMode=AOT]". Mode-agnostic tests get no tag.
public class TagTestModeAttribute : Attribute, ITestDiscoveryEventReceiver
{
    public const string PropertyName = "EngineTestMode";

    public int Order => 0;

    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        foreach (var argument in context.TestContext.Metadata.TestDetails.TestClassArguments)
        {
            if (argument is TestMode testMode)
            {
                context.AddProperty(PropertyName, testMode.ToString());
                break;
            }
        }

        return default(ValueTask);
    }
}
