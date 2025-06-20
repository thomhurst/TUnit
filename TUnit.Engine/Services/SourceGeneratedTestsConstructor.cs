using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;

namespace TUnit.Engine.Services;

internal class SourceGeneratedTestsConstructor(IExtension extension,
    TestsCollector testsCollector,
    DependencyCollector dependencyCollector,
    ContextManager contextManager,
    IServiceProvider serviceProvider) : BaseTestsConstructor(extension, dependencyCollector)
{
    private readonly UnifiedTestBuilder _unifiedBuilder = new(contextManager, serviceProvider);

    protected override async Task<DiscoveredTest[]> DiscoverTestsAsync(ExecuteRequestContext context)
    {
        var discoveredTests = new List<DiscoveredTest>();

        var discoveryResult = await testsCollector.DiscoverTestsAsync();
        var (tests, failures) = _unifiedBuilder.BuildTests(discoveryResult);
        discoveredTests.AddRange(tests);

        foreach (var failure in failures)
        {
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, new TestNode
            {
                DisplayName = failure.TestMethodName, Uid = new TestNodeUid(failure.TestId), Properties = new PropertyBag(new ErrorTestNodeStateProperty(failure.Exception))
            }));
        }

        foreach (var dynamicTest in testsCollector.GetDynamicTests())
        {
            discoveredTests.AddRange(_unifiedBuilder.BuildTests(dynamicTest));
        }

        return discoveredTests.ToArray();
    }
}
