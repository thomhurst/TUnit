using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using TUnit.TestAdapter.Constants;

namespace TUnit.TestAdapter;

[FileExtension(".dll")]
[FileExtension(".exe")]
[DefaultExecutorUri(TestAdapterConstants.ExecutorUriString)]
[ExtensionUri(TestAdapterConstants.ExecutorUriString)]
public class TestExecutor : ITestExecutor2
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly AsyncTestExecutor _asyncTestExecutor;
    
    public TestExecutor()
    {
        _asyncTestExecutor = new AsyncTestExecutor(_cancellationTokenSource);
    }

    public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        RunTests(tests?.Select(x => x.Source), runContext, frameworkHandle);
    }

    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (sources is null)
        {
            return;
        }

        var tests = new TestCollector(frameworkHandle).TestsFromSources(sources);
        
        _asyncTestExecutor.RunInAsyncContext(tests, runContext, frameworkHandle).GetAwaiter().GetResult();
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
}