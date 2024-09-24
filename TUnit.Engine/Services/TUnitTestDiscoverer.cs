using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine.Services;

internal class TUnitTestDiscoverer
{
    private readonly TestsLoader _testsLoader;
    private readonly TestFilterService _testFilterService;
    private readonly ILogger<TUnitTestDiscoverer> _logger;

    public TUnitTestDiscoverer(TestsLoader testsLoader, TestFilterService testFilterService, ILoggerFactory loggerFactory)
    {
        _testsLoader = testsLoader;
        _testFilterService = testFilterService;
        _logger = loggerFactory.CreateLogger<TUnitTestDiscoverer>();
    }
    
    public DiscoveredTest[] DiscoverTests(TestExecutionRequest? discoverTestExecutionRequest, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var tests = _testsLoader.GetTests();

        var discoveredTests = _testFilterService.FilterTests(discoverTestExecutionRequest?.Filter, tests).ToArray();
        
        _logger.LogTrace($"Found {discoveredTests.Length} tests after filtering.");
        
        return discoveredTests;
    }

    public IReadOnlyCollection<FailedInitializationTest> GetFailedToInitializeTests()
    {
        var failedToInitializeTests = TestDictionary.GetFailedToInitializeTests();

        _logger.LogTrace($"{failedToInitializeTests.Length} tests failed to initialize.");
        
        return failedToInitializeTests;
    }
}