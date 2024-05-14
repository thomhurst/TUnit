using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine;

internal class TUnitTestDiscoverer
{
    private readonly TestsLoader _testsLoader;
    private readonly TestFilterService _testFilterService;

    public TUnitTestDiscoverer(TestsLoader testsLoader, TestFilterService testFilterService)
    {
        _testsLoader = testsLoader;
        _testFilterService = testFilterService;
    }
    
    public IEnumerable<TestInformation> DiscoverTests(TestExecutionRequest? discoverTestExecutionRequest, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var tests = _testsLoader.GetTests();

        return _testFilterService.FilterTests(discoverTestExecutionRequest?.Filter, tests);
    }
}