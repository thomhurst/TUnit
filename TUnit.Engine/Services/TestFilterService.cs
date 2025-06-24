#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Services;

internal class TestFilterService(ILoggerFactory loggerFactory)
{
    private readonly ILogger<TestFilterService> _logger = loggerFactory.CreateLogger<TestFilterService>();

    public IReadOnlyCollection<ExecutableTest> FilterTests(TestExecutionRequest? testExecutionRequest, IReadOnlyCollection<ExecutableTest> testNodes)
    {
        var testExecutionFilter = testExecutionRequest?.Filter;
        
        if (testExecutionFilter is null or NopFilter)
        {
            _logger.LogTrace("No test filter found.");

            if (testExecutionRequest is RunTestExecutionRequest)
            {
                // Return all tests since we don't have Attributes on TestDetails
                return testNodes.ToArray();
            }

            return testNodes;
        }
        
        _logger.LogTrace($"Test filter is: {testExecutionFilter.GetType().Name}");

        var filteredTests = testNodes.Where(x => MatchesTest(testExecutionFilter, x)).ToArray();
        
        return filteredTests;
    }

    public bool MatchesTest(ITestExecutionFilter? testExecutionFilter, ExecutableTest executableTest)
    {
#pragma warning disable TPEXP
        var shouldRunTest = testExecutionFilter switch
        {
            null => true,
            NopFilter => true,
            TestNodeUidListFilter testNodeUidListFilter => testNodeUidListFilter.TestNodeUids.Contains(new TestNodeUid(executableTest.TestId)),
            TreeNodeFilter treeNodeFilter => treeNodeFilter.MatchesFilter(BuildPath(executableTest), BuildPropertyBag(executableTest)),
            _ => UnhandledFilter(testExecutionFilter)
        };

        return shouldRunTest;
#pragma warning restore TPEXP
    }

    private string BuildPath(ExecutableTest test)
    {
        var metadata = test.Metadata;
        var assembly = metadata.TestClassType.Assembly.GetName();
        var classTypeName = metadata.TestClassType.Name;
        
        return $"/{assembly.Name ?? assembly.FullName}/{metadata.TestClassType.Namespace}/{classTypeName}/{metadata.TestMethodName}";
    }

    private PropertyBag BuildPropertyBag(ExecutableTest test)
    {
        var properties = new List<IProperty>();
        
        // Add categories
        foreach (var category in test.Metadata.Categories)
        {
            properties.Add(new TestMetadataProperty(category));
            properties.Add(new KeyValuePairStringProperty("Category", category));
        }
        
        return new PropertyBag(properties);
    }

    private bool UnhandledFilter(ITestExecutionFilter testExecutionFilter)
    {
        _logger.LogWarning($"Filter is Unhandled Type: {testExecutionFilter.GetType().FullName}");
        return true;
    }
}
