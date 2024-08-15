using System.Collections.Frozen;
using Microsoft.Testing.Platform.Requests;

namespace TUnit.Engine.Services;

internal class TUnitTestDiscoverer
{
    private readonly TestsLoader _testsLoader;
    private readonly TestFilterService _testFilterService;

    public TUnitTestDiscoverer(TestsLoader testsLoader, TestFilterService testFilterService)
    {
        _testsLoader = testsLoader;
        _testFilterService = testFilterService;
    }
    
    public DiscoveredTest[] DiscoverTests(TestExecutionRequest? discoverTestExecutionRequest, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var tests = _testsLoader.GetTests();

        return _testFilterService.FilterTests(discoverTestExecutionRequest?.Filter, tests).ToArray();
    }
}