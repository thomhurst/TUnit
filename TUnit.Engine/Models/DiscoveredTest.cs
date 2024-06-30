using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Models;

internal record DiscoveredTest
{
    public TestContext TestContext { get; }

    public DiscoveredTest(TestContext testContext)
    {
        TestContext = testContext;
    }

    public TestInformation TestInformation => TestContext.TestInformation;

    public TestNode TestNode => TestInformation.ToTestNode();
}