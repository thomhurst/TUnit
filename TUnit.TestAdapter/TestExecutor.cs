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
public class TestExecutor : ITestExecutor2
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public void RunTests(IEnumerable<TestCase>? testCases, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (testCases is null)
        {
            return;
        }
        
        var serviceProvider = BuildServices(runContext, frameworkHandle);
            
        serviceProvider.GetRequiredService<AsyncTestRunExecutor>()
            .RunInAsyncContext(Filter(testCases, serviceProvider))
            .GetAwaiter()
            .GetResult();
    }

    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (sources is null)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Warning, "No sources found");
            return;
        }
        
        var serviceProvider = BuildServices(runContext, frameworkHandle);

        var tests = serviceProvider.GetRequiredService<TestCollector>()
            .TestsFromSources(sources)
            .Select(x => x.ToTestCase());
        
        serviceProvider.GetRequiredService<AsyncTestRunExecutor>()
            .RunInAsyncContext(Filter(tests, serviceProvider))
            .GetAwaiter()
            .GetResult();
    }

    private IEnumerable<TestCase> Filter(IEnumerable<TestCase> tests, IServiceProvider serviceProvider)
    {
        var testFilterProvider = serviceProvider.GetRequiredService<TUnitTestFilterProvider>();
        
        return testFilterProvider.FilterTests(tests);
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    public bool ShouldAttachToTestHost(IEnumerable<TestCase>? tests, IRunContext runContext)
    {
        return ShouldAttachToTestHost(tests?.Select(x => x.Source), runContext);
    }

    public bool ShouldAttachToTestHost(IEnumerable<string>? sources, IRunContext runContext)
    {
        return runContext.IsBeingDebugged;
    }

    private IServiceProvider BuildServices(IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        return new ServiceCollection()
            .AddSingleton(runContext ?? new NoOpRunContext())
            .AddSingleton(frameworkHandle ?? new NoOpFrameworkHandle())
            .AddSingleton<ITestExecutionRecorder>(x => x.GetRequiredService<IFrameworkHandle>())
            .AddSingleton<IMessageLogger>(x => x.GetRequiredService<IFrameworkHandle>())
            .AddSingleton(_cancellationTokenSource)
            .AddTestEngineServices()
            .BuildServiceProvider();
    }
}