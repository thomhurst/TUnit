using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine;

internal record DiscoveredTest
{
    public DiscoveredTest(TestInformation testInformation)
    {
        TestInformation = testInformation;
        TestNode = testInformation.ToTestNode();
    }

    public TestInformation TestInformation { get; }

    public TestNode TestNode { get; }
}