using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions;
using TUnit.Core;

namespace TUnit.Engine.Services;

internal class SourceGeneratedTestsConstructor(IExtension extension,
    TestsCollector testsCollector,
    DependencyCollector dependencyCollector,
    ContextManager contextManager,
    IServiceProvider serviceProvider) : BaseTestsConstructor(extension, dependencyCollector, contextManager, serviceProvider)
{
    protected override async Task<DiscoveredTest[]> DiscoverTestsAsync()
    {
        var testMetadatas = new List<TestMetadata>();
        await foreach (var testMetadata in testsCollector.GetTestsAsync())
        {
            testMetadatas.Add(testMetadata);
        }

        var dynamicTests = testsCollector.GetDynamicTests();

        var discoveredTests = testMetadatas
            .Select(ConstructTest)
            .Concat(dynamicTests.SelectMany(ConstructTests))
            .ToArray();

        return discoveredTests;
    }
}
