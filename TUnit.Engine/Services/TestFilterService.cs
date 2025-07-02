#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;

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
            TreeNodeFilter treeNodeFilter => CheckTreeNodeFilter(treeNodeFilter, executableTest),
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

        return $"/{assembly.Name ?? "*"}/{metadata.TestClassType.Namespace ?? "*"}/{classTypeName}/{metadata.TestMethodName}";
    }

    private bool CheckTreeNodeFilter(
#pragma warning disable TPEXP
        TreeNodeFilter treeNodeFilter,
#pragma warning restore TPEXP
        ExecutableTest executableTest)
    {
        var path = BuildPath(executableTest);
        var propertyBag = BuildPropertyBag(executableTest);
        _logger.LogDebug($"Checking TreeNodeFilter for path: {path}");

        var matches = treeNodeFilter.MatchesFilter(path, propertyBag);
        _logger.LogDebug($"Filter match result: {matches}");

        return matches;
    }

    private bool UnhandledFilter(ITestExecutionFilter testExecutionFilter)
    {
        _logger.LogWarning($"Filter is Unhandled Type: {testExecutionFilter.GetType().FullName}");
        return true;
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

        if (test.Context?.TestDetails.CustomProperties == null)
        {
            return new PropertyBag(properties);
        }

        foreach (var propertyEntry in test.Context.TestDetails.CustomProperties)
        {
            properties.AddRange(propertyEntry.Value.Select(value => new KeyValuePairStringProperty(propertyEntry.Key, value)).Cast<IProperty>());
        }

        return new PropertyBag(properties);
    }
}
