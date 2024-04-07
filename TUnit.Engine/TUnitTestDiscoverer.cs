using System.Reflection;
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
    
    public IEnumerable<TestNode> DiscoverTests(TestExecutionRequest? discoverTestExecutionRequest,
        Func<IEnumerable<Assembly>> testAssemblies, CancellationToken cancellationToken)
    {
        var assemblies = testAssemblies();
        
        foreach (var assembly in assemblies.Select(x => new CachedAssemblyInformation(x)))
        {
            foreach (var testDetails in _testsLoader.GetTests())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                var testNode = testDetails.ToTestNode();

                if (_testFilterService.MatchesTest(discoverTestExecutionRequest?.Filter, testNode))
                {
                    yield return testNode;
                }
            }
        }
    }
}