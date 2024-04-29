using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Extensions;

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
        
        return _testsLoader.GetTests()
            .Where(testDetails => _testFilterService.MatchesTest(discoverTestExecutionRequest?.Filter, testDetails));
    }
}