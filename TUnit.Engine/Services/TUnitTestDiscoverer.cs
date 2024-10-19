using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine.Services;

internal class TUnitTestDiscoverer(
    TestsLoader testsLoader,
    TestFilterService testFilterService,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger<TUnitTestDiscoverer> _logger = loggerFactory.CreateLogger<TUnitTestDiscoverer>();

    public DiscoveredTest[] DiscoverTests(TestExecutionRequest? discoverTestExecutionRequest, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var tests = testsLoader.GetTests();

        var discoveredTests = testFilterService.FilterTests(discoverTestExecutionRequest?.Filter, tests).ToArray();
        
        _logger.LogTrace($"Found {discoveredTests.Length} tests after filtering.");
        
        return discoveredTests;
    }

    public IReadOnlyCollection<FailedInitializationTest> GetFailedToInitializeTests()
    {
        var failedToInitializeTests = TestDictionary.GetFailedToInitializeTests();

        _logger.LogWarning($"{failedToInitializeTests.Length} tests failed to initialize.");
        
        return failedToInitializeTests;
    }
}