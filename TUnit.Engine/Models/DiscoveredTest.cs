using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Models;

internal record DiscoveredTest
{
    public UnInvokedTest UnInvokedTest { get; }

    public DiscoveredTest(UnInvokedTest unInvokedTest)
    {
        UnInvokedTest = unInvokedTest;
    }

    public TestInformation TestInformation => TestContext.TestInformation;
    
    public TestContext TestContext => UnInvokedTest.TestContext;

    public TestNode TestNode => TestInformation.ToTestNode();
}