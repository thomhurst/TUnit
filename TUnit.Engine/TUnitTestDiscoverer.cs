using System.Reflection;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine;
using TUnit.Engine.Extensions;

namespace TUnit.TestAdapter;

internal class TUnitTestDiscoverer
{
    private readonly TestsLoader _testsLoader;

    public TUnitTestDiscoverer(TestsLoader testsLoader)
    {
        _testsLoader = testsLoader;
    }
    
    public IEnumerable<TestNode> DiscoverTests(DiscoverTestExecutionRequest discoverTestExecutionRequest,
        Func<IEnumerable<Assembly>> testAssemblies, CancellationToken cancellationToken)
    {
        var filter = discoverTestExecutionRequest.Filter as TestNodeUidListFilter ?? new TestNodeUidListFilter([]);

        var assemblies = testAssemblies();
        
        foreach (var assembly in assemblies.Select(x => new CachedAssemblyInformation(x)))
        {
            foreach (var testDetails in _testsLoader.GetTests(assembly))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                var testNode = testDetails.ToTestNode();

                if (filter.TestNodeUids.Contains(testNode.Uid))
                {
                    yield return testNode;
                }
            }
        }
    }
}