using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Models;

internal record DiscoveredTest
{
    public DiscoveredTest(TestInformation testInformation)
    {
        TestInformation = testInformation;
    }

    public TestInformation TestInformation { get; }

    public TestNode TestNode => TestInformation.ToTestNode();
}