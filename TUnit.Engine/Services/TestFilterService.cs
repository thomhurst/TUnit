#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

internal class TestFilterService(TUnitFrameworkLogger logger, TestArgumentTrackingService testArgumentTrackingService)
{
    public IReadOnlyCollection<AbstractExecutableTest> FilterTests(ITestExecutionFilter? testExecutionFilter, IReadOnlyCollection<AbstractExecutableTest> testNodes)
    {
        if (testExecutionFilter is null or NopFilter)
        {
            logger.LogTrace("No test filter found.");

            // When no filter is specified, exclude explicit tests
            return FilterOutExplicitTests(testNodes);
        }

        logger.LogTrace($"Test filter is: {testExecutionFilter.GetType().Name}");

        // Apply the filter and separate explicit from non-explicit tests in one pass
        var filteredTests = new List<AbstractExecutableTest>();
        var filteredExplicitTests = new List<AbstractExecutableTest>();
        
        foreach (var test in testNodes)
        {
            if (MatchesTest(testExecutionFilter, test))
            {
                if (IsExplicitTest(test))
                {
                    filteredExplicitTests.Add(test);
                }
                else
                {
                    filteredTests.Add(test);
                }
            }
        }

        // Special handling for explicit tests based on filter type and results:
        // 
        // If we have both explicit and non-explicit tests that match:
        // - For property-based filters (like [Category!=Performance]), include both types
        //   as the explicit tests legitimately match the filter criteria
        // - For path-based wildcard filters, exclude explicit tests as they weren't specifically targeted
        // 
        // If we only have explicit tests, they were specifically targeted by the filter
        
        if (filteredTests.Count > 0 && filteredExplicitTests.Count > 0)
        {
            // Check if this is a property-based filter that should include explicit tests
            if (IsPropertyBasedFilter(testExecutionFilter))
            {
                logger.LogTrace($"Property-based filter matched both explicit ({filteredExplicitTests.Count}) and non-explicit ({filteredTests.Count}) tests. Including both.");
                var allTests = new List<AbstractExecutableTest>();
                allTests.AddRange(filteredTests);
                allTests.AddRange(filteredExplicitTests);
                return allTests;
            }
            else
            {
                logger.LogTrace($"Path-based filter matched both explicit and non-explicit tests. Excluding {filteredExplicitTests.Count} explicit tests.");
                return filteredTests;
            }
        }

        // If we only have explicit tests, the filter was specifically targeting them
        if (filteredExplicitTests.Count > 0)
        {
            logger.LogTrace($"Filter matched only explicit tests. Running {filteredExplicitTests.Count} explicit tests.");
            return filteredExplicitTests;
        }

        // Otherwise, return the non-explicit tests
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

        // First, invoke the global test argument tracking service to track shared instances
        await testArgumentTrackingService.OnTestRegistered(registeredContext);

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

    private bool IsPropertyBasedFilter(ITestExecutionFilter? testExecutionFilter)
    {
#pragma warning disable TPEXP
        if (testExecutionFilter is TreeNodeFilter treeNodeFilter)
        {
            // A filter is considered property-based if it contains property conditions
            // like [Category!=Performance] or [Property=Value]
            var filter = treeNodeFilter.Filter;
            return filter.Contains('[') && filter.Contains('=');
        }
#pragma warning restore TPEXP
        
        return false;
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

    private bool IsExplicitTest(AbstractExecutableTest test)
    {
        // Check if the test or its class has the ExplicitAttribute
        // First check the aggregated attributes (should contain both method and class attributes)
        if (test.Context.TestDetails.Attributes.OfType<ExplicitAttribute>().Any())
        {
            return true;
        }

        // Also check the class type directly as a fallback
        var testClassType = test.Context.TestDetails.ClassType;
        return testClassType.GetCustomAttributes(typeof(ExplicitAttribute), true).Length > 0;
    }

    private IReadOnlyCollection<AbstractExecutableTest> FilterOutExplicitTests(IReadOnlyCollection<AbstractExecutableTest> testNodes)
    {
        var filteredTests = new List<AbstractExecutableTest>();
        
        foreach (var test in testNodes)
        {
            if (!IsExplicitTest(test))
            {
                filteredTests.Add(test);
            }
            else
            {
                logger.LogTrace($"Test {test.TestId} is explicit and no filter was specified, skipping.");
            }
        }
        
        return filteredTests;
    }
}
