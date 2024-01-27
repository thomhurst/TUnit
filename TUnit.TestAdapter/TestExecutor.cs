using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.TestAdapter.Constants;
using TUnit.TestAdapter.Extensions;
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
        
        var testsWithTestCases = 
            serviceProvider.GetRequiredService<TestCollector>()
                .TestsFromTestCases(testCases);
            
        serviceProvider.GetRequiredService<AsyncTestRunExecutor>()
            .RunInAsyncContext(testsWithTestCases)
            .GetAwaiter()
            .GetResult();
    }

    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (sources is null)
        {
            return;
        }
        
        var serviceProvider = BuildServices(runContext, frameworkHandle);

        var tests = serviceProvider.GetRequiredService<TestCollector>()
            .TestsFromSources(sources)
            .Select(x => new TestWithTestCase(x, x.ToTestCase()));
            
        serviceProvider.GetRequiredService<AsyncTestRunExecutor>()
            .RunInAsyncContext(tests)
            .GetAwaiter()
            .GetResult();
        
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
            .AddTestAdapterServices()
            .BuildServiceProvider();
    }
}