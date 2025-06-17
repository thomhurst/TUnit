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
        
        // Process source-generated tests
        var discoveryResult = await testsCollector.DiscoverTestsAsync();
        var (tests, failures) = _unifiedBuilder.BuildTests(discoveryResult);
        discoveredTests.AddRange(tests);
        
        // TODO: Handle discovery failures appropriately
        foreach (var failure in failures)
        {
            // For now, log to console. In the future, this should be reported properly
            Console.WriteLine($"Test discovery failed: {failure.TestId ?? "Unknown"} - {failure.Reason}");
        }
        
        // Process dynamic tests
        foreach (var dynamicTest in testsCollector.GetDynamicTests())
        {
            discoveredTests.AddRange(_unifiedBuilder.BuildTests(dynamicTest));
        }
        
        return discoveredTests.ToArray();
    }
}
