#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

internal class TestFilterService(TUnitFrameworkLogger logger)
{
    public IReadOnlyCollection<AbstractExecutableTest> FilterTests(ITestExecutionFilter? testExecutionFilter, IReadOnlyCollection<AbstractExecutableTest> testNodes)
    {
        if (testExecutionFilter is null or NopFilter)
        {
            logger.LogTrace("No test filter found.");

            return testNodes;
        }

        logger.LogTrace($"Test filter is: {testExecutionFilter.GetType().Name}");

        var filteredTests = new List<AbstractExecutableTest>();
        foreach (var test in testNodes)
        {
            if (MatchesTest(testExecutionFilter, test))
            {
                filteredTests.Add(test);
            }
        }

        return filteredTests;
    }

    private async Task RegisterTest(AbstractExecutableTest test)
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

        var eventObjects = test.Context.GetEligibleEventObjects();

        foreach (var receiver in eventObjects.OfType<ITestRegisteredEventReceiver>())
        {
            try
            {
                await receiver.OnTestRegistered(registeredContext);
            }
            catch (Exception ex)
            {
                await logger.LogErrorAsync($"Error in test registered event receiver: {ex.Message}");
                throw;
            }
        }

    }

    public async Task RegisterTestsAsync(IEnumerable<AbstractExecutableTest> tests)
    {
        foreach (var test in tests)
        {
            await RegisterTest(test);
        }
    }

    public bool MatchesTest(ITestExecutionFilter? testExecutionFilter, AbstractExecutableTest executableTest)
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

    private string BuildPath(AbstractExecutableTest test)
    {
        var metadata = test.Metadata;

        var classMetadata = test.Context.TestDetails.MethodMetadata.Class;
        var assemblyName = classMetadata.Assembly.Name ?? metadata.TestClassType.Assembly.GetName().Name ?? "*";
        var namespaceName = classMetadata.Namespace ?? "*";
        var classTypeName = classMetadata.Name;

        var path = $"/{assemblyName}/{namespaceName}/{classTypeName}/{metadata.TestMethodName}";


        return path;
    }

    private bool CheckTreeNodeFilter(
#pragma warning disable TPEXP
        TreeNodeFilter treeNodeFilter,
#pragma warning restore TPEXP
        AbstractExecutableTest executableTest)
    {
        var path = BuildPath(executableTest);

        var propertyBag = BuildPropertyBag(executableTest);

        var matches = treeNodeFilter.MatchesFilter(path, propertyBag);

        if (!matches)
        {
            logger.LogTrace($"Test {executableTest.TestId} with path '{path}' did not match treenode filter");
        }

        return matches;
    }

    private bool UnhandledFilter(ITestExecutionFilter testExecutionFilter)
    {
        logger.LogWarning($"Filter is Unhandled Type: {testExecutionFilter.GetType().FullName}");
        return true;
    }

    private PropertyBag BuildPropertyBag(AbstractExecutableTest test)
    {
        var properties = new List<IProperty>();

        foreach (var category in test.Context.TestDetails.Categories)
        {
            properties.Add(new TestMetadataProperty(category));
            properties.Add(new TestMetadataProperty("Category", category));
        }

        foreach (var propertyEntry in test.Context.TestDetails.CustomProperties)
        {
            properties.AddRange(propertyEntry.Value.Select(value => new TestMetadataProperty(propertyEntry.Key, value)));
        }

        return new PropertyBag(properties);
    }
}
