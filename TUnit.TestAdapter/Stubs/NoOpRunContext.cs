using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace TUnit.TestAdapter.Stubs;

public class NoOpRunContext : IRunContext
{
    public IRunSettings? RunSettings { get; } = new RunSettings();
    
    public ITestCaseFilterExpression? GetTestCaseFilter(IEnumerable<string>? supportedProperties, Func<string, TestProperty?> propertyProvider)
    {
        return new TestCaseFilterExpression(new FilterExpressionWrapper(string.Empty));
    }

    public bool KeepAlive => false;
    public bool InIsolation => false;
    public bool IsDataCollectionEnabled => false;
    public bool IsBeingDebugged => false;
    public string? TestRunDirectory { get; } = Directory.GetCurrentDirectory();
    public string? SolutionDirectory { get; } = Directory.GetCurrentDirectory();
}