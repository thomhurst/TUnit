#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

internal class TestFilterService(TUnitFrameworkLogger logger)
{
    private readonly TUnitFrameworkLogger _logger = logger;
    public IReadOnlyCollection<ExecutableTest> FilterTests(ITestExecutionFilter? testExecutionFilter, IReadOnlyCollection<ExecutableTest> testNodes)
    {
        if (testExecutionFilter is null or NopFilter)
        {
            _logger.LogTrace("No test filter found.");

            return testNodes;
        }

        _logger.LogTrace($"Test filter is: {testExecutionFilter.GetType().Name}");

        // Create pre-sized list if we can estimate the size
        var filteredTests = new List<ExecutableTest>();
        foreach (var test in testNodes)
        {
            if (MatchesTest(testExecutionFilter, test))
            {
                filteredTests.Add(test);
            }
        }

        return filteredTests;
    }

    private async Task<List<ExecutableTest>> ApplyFilterAsync(List<ExecutableTest> tests, ITestExecutionFilter filter)
    {
        // Debug: Applying filter to {tests.Count} tests of type {filter.GetType().Name}

        var filteredTests = new List<ExecutableTest>();
        foreach (var test in tests)
        {
            if (MatchesTest(filter, test))
            {
                filteredTests.Add(test);
            }
        }

        // Debug: Filter matched {filteredTests.Count} tests

        var testsToInclude = new HashSet<ExecutableTest>(filteredTests);
        var processedTests = new HashSet<string>();
        var queue = new Queue<ExecutableTest>(filteredTests);

        while (queue.Count > 0)
        {
            var currentTest = queue.Dequeue();
            if (!processedTests.Add(currentTest.TestId))
            {
                continue;
            }

            foreach (var dependency in currentTest.Dependencies)
            {
                if (testsToInclude.Add(dependency))
                {
                    queue.Enqueue(dependency);
                }
            }
        }

        await _logger.LogAsync(LogLevel.Debug,
            $"After including dependencies: {testsToInclude.Count} tests will be executed",
            null,
            (state, _) => state);

        var resultList = testsToInclude.ToList();
        foreach (var test in resultList)
        {
            await RegisterTest(test);
        }

        return resultList;
    }

    private async Task RegisterTest(ExecutableTest test)
    {
        var discoveredTest = new DiscoveredTest<object>
        {
            TestContext = test.Context
        };

        var registeredContext = new TestRegisteredContext(test.Context)
        {
            DiscoveredTest = discoveredTest
        };

        test.Context.InternalDiscoveredTest = discoveredTest;

        var attributes = test.Context.TestDetails.Attributes;

        foreach (var attribute in attributes)
        {
            if (attribute is ITestRegisteredEventReceiver receiver)
            {
                try
                {
                    await receiver.OnTestRegistered(registeredContext);
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Error in test registered event receiver: {ex.Message}");
                }
            }
        }

        // Register test for execution (keeping original functionality)
    }

    public async Task RegisterTestsAsync(IEnumerable<ExecutableTest> tests)
    {
        foreach (var test in tests)
        {
            await RegisterTest(test);
        }
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

        // For generic types, use the ClassMetadata which has the correct name

        var classMetadata = test.Context.TestDetails.MethodMetadata.Class;
        var assemblyName = classMetadata.Assembly?.Name ?? metadata.TestClassType.Assembly.GetName().Name ?? "*";
        var namespaceName = classMetadata.Namespace ?? "*";
        var classTypeName = classMetadata.Name;

        return $"/{assemblyName}/{namespaceName}/{classTypeName}/{metadata.TestMethodName}";
    }

    private bool CheckTreeNodeFilter(
#pragma warning disable TPEXP
        TreeNodeFilter treeNodeFilter,
#pragma warning restore TPEXP
        ExecutableTest executableTest)
    {
        try
        {
            if (executableTest.Context.TestDetails.MethodMetadata.Class.Name == "AllDataSourcesCombinedTests")
            {
                Console.Write("");
            }
            var path = BuildPath(executableTest);
            var propertyBag = BuildPropertyBag(executableTest);
            _logger.LogDebug($"Checking TreeNodeFilter for path: {path}");

            // Additional debug for generic tests
            if (executableTest.Context.TestDetails.MethodMetadata.Class.Name == "SimpleGenericClassTests")
            {
                Console.WriteLine($"DEBUG: Built path for generic test: {path}");
                Console.WriteLine($"DEBUG: Test display name: {executableTest.DisplayName}");
            }

            var matches = treeNodeFilter.MatchesFilter(path, propertyBag);
            _logger.LogDebug($"Filter match result: {matches}");

            return matches;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
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

        foreach (var propertyEntry in test.Context.TestDetails.CustomProperties)
        {
            properties.AddRange(propertyEntry.Value.Select(value => new KeyValuePairStringProperty(propertyEntry.Key, value)));
        }

        return new PropertyBag(properties);
    }
}
