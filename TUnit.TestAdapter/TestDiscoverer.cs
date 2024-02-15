using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Engine;
using TUnit.Engine.Constants;
using TUnit.Engine.Extensions;
using TUnit.TestAdapter.Stubs;

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
        var testCollector = BuildServices(discoveryContext, logger)
            .GetRequiredService<TestCollector>();

        var tests = testCollector.TestsFromSources(sources).ToList();
        
        if (!Debugger.IsAttached)
        {
            //Debugger.Launch();
        }
        
        foreach (var test in tests)
        {
            logger.SendMessage(TestMessageLevel.Informational, "Test found: " + test.TestNameWithArguments);
            discoverySink.SendTestCase(test.ToTestCase());
        }
    }
    
    private IServiceProvider BuildServices(IDiscoveryContext discoveryContext, IMessageLogger messageLogger)
    {
        return new ServiceCollection()
            .AddSingleton(discoveryContext)
            .AddSingleton(messageLogger)
            .AddSingleton<ITestExecutionRecorder, NoOpExecutionRecorder>()
            .AddTestEngineServices()
            .BuildServiceProvider();
    }
}