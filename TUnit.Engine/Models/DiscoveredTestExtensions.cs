using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Models;

internal static class DiscoveredTestExtensions
{
    public static TestNode ToTestNode(this DiscoveredTest discoveredTest) => discoveredTest.TestContext.ToTestNode();
}