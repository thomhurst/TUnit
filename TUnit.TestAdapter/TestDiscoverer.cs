using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.TestAdapter.Constants;
using TUnit.TestAdapter.Extensions;

namespace TUnit.TestAdapter;

[FileExtension(".dll")]
[FileExtension(".exe")]
[DefaultExecutorUri(TestAdapterConstants.ExecutorUriString)]
[ExtensionUri(TestAdapterConstants.ExecutorUriString)]
public class TestDiscoverer : ITestDiscoverer
{
    public void DiscoverTests(IEnumerable<string> sources, 
        IDiscoveryContext discoveryContext, 
        IMessageLogger logger,
        ITestCaseDiscoverySink discoverySink)
    {
        var testCollector = new TestCollector(logger);
        
        foreach (var test in testCollector.TestsFromSources(sources))
        {
            logger.SendMessage(TestMessageLevel.Informational, "Test found: " + test.FullName);
            discoverySink.SendTestCase(test.ToTestCase());
        }
    }
}