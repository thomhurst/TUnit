using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
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
    
    public IEnumerable<TestNode> DiscoverTests(TestExecutionRequest? discoverTestExecutionRequest, CancellationToken cancellationToken)
    {
        foreach (var testDetails in _testsLoader.GetTests())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var testNode = testDetails.ToTestNode();
            testNode.Properties.Add(DiscoveredTestNodeStateProperty.CachedInstance);

            if (_testFilterService.MatchesTest(discoverTestExecutionRequest?.Filter, testNode))
            {
                yield return testNode;
            }
        }
    }
}