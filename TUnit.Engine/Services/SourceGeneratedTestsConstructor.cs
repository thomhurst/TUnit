using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions;
using TUnit.Core;

namespace TUnit.Engine.Services;

internal class SourceGeneratedTestsConstructor(IExtension extension,
    TestsCollector testsCollector,
    DependencyCollector dependencyCollector,
    ContextManager contextManager,
    IServiceProvider serviceProvider) : BaseTestsConstructor(extension, dependencyCollector)
{
    private readonly UnifiedTestBuilder _unifiedBuilder = new(contextManager, serviceProvider);
    
    protected override async Task<DiscoveredTest[]> DiscoverTestsAsync()
    {
        var discoveredTests = new List<DiscoveredTest>();
        
        // Process regular test metadata
        await foreach (var testMetadata in testsCollector.GetTestsAsync())
        {
            discoveredTests.Add(_unifiedBuilder.BuildTest(testMetadata));
        }
        
        // Process dynamic tests
        foreach (var dynamicTest in testsCollector.GetDynamicTests())
        {
            discoveredTests.AddRange(_unifiedBuilder.BuildTests(dynamicTest));
        }
        
        return discoveredTests.ToArray();
    }
}
