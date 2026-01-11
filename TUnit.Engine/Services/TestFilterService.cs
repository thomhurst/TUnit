#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

internal class TestFilterService(TUnitFrameworkLogger logger, TestArgumentRegistrationService testArgumentRegistrationService)
{
    public IReadOnlyCollection<AbstractExecutableTest> FilterTests(ITestExecutionFilter? testExecutionFilter, IReadOnlyCollection<AbstractExecutableTest> testNodes)
    {
        if (testExecutionFilter is null or NopFilter)
        {
            logger.LogTrace("No test filter found.");

            return FilterOutExplicitTests(testNodes);
        }

        logger.LogTrace($"Test filter is: {testExecutionFilter.GetType().Name}");

        // Pre-allocate capacity to avoid resizing during filtering
        var capacity = testNodes is ICollection<AbstractExecutableTest> col ? col.Count : 16;
        var filteredTests = new List<AbstractExecutableTest>(capacity);
        var filteredExplicitTests = new List<AbstractExecutableTest>(capacity / 4); // Estimate ~25% explicit tests

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

        if (filteredTests.Count > 0)
        {
            logger.LogTrace($"Filter matched {filteredTests.Count} non-explicit tests. Excluding {filteredExplicitTests.Count} explicit tests.");
            return filteredTests;
        }

        if (filteredExplicitTests.Count > 0)
        {
            logger.LogTrace($"Filter matched only explicit tests. Running {filteredExplicitTests.Count} explicit tests.");
            return filteredExplicitTests;
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

        try
        {
            await testArgumentRegistrationService.RegisterTestArgumentsAsync(test.Context);
        }
        catch (Exception ex)
        {
            // Mark the test as failed and skip further event receiver processing
            test.SetResult(TestState.Failed, ex);
            return;
        }

        // Use pre-computed receivers (already filtered, sorted, and scoped-attribute filtered)
        foreach (var receiver in test.Context.GetTestRegisteredReceivers())
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
        // Return cached path if available
        if (test.CachedFilterPath != null)
        {
            return test.CachedFilterPath;
        }

        var metadata = test.Metadata;

        var classMetadata = test.Context.Metadata.TestDetails.MethodMetadata.Class;
        var assemblyName = classMetadata.Assembly.Name ?? metadata.TestClassType.Assembly.GetName().Name ?? "*";
        var namespaceName = classMetadata.Namespace ?? "*";
        var classTypeName = classMetadata.Name;

        var path = $"/{assemblyName}/{namespaceName}/{classTypeName}/{metadata.TestMethodName}";

        // Cache the path for future calls
        test.CachedFilterPath = path;

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
        // Return cached PropertyBag if available
        if (test.CachedPropertyBag is PropertyBag cachedBag)
        {
            return cachedBag;
        }

        // Pre-calculate capacity: 2 properties per category + custom properties
        var categoryCount = test.Context.Metadata.TestDetails.Categories.Count;
        var customPropCount = test.Context.Metadata.TestDetails.CustomProperties.Sum(p => p.Value.Count);
        var properties = new List<IProperty>(categoryCount * 2 + customPropCount);

        foreach (var category in test.Context.Metadata.TestDetails.Categories)
        {
            properties.Add(new TestMetadataProperty(category));
            properties.Add(new TestMetadataProperty("Category", category));
        }

        // Replace LINQ with manual loop for better performance in hot path
        foreach (var propertyEntry in test.Context.Metadata.TestDetails.CustomProperties)
        {
            foreach (var value in propertyEntry.Value)
            {
                properties.Add(new TestMetadataProperty(propertyEntry.Key, value));
            }
        }

        var propertyBag = new PropertyBag(properties);

        // Cache the PropertyBag for future calls
        test.CachedPropertyBag = propertyBag;

        return propertyBag;
    }

    private bool IsExplicitTest(AbstractExecutableTest test)
    {
        if (test.Context.Metadata.TestDetails.HasAttribute<ExplicitAttribute>())
        {
            return true;
        }

        var testClassType = test.Context.Metadata.TestDetails.ClassType;
        return testClassType.GetCustomAttributes(typeof(ExplicitAttribute), true).Length > 0;
    }

    private IReadOnlyCollection<AbstractExecutableTest> FilterOutExplicitTests(IReadOnlyCollection<AbstractExecutableTest> testNodes)
    {
        // Pre-allocate assuming most tests are not explicit
        var capacity = testNodes is ICollection<AbstractExecutableTest> col ? col.Count : testNodes.Count;
        var filteredTests = new List<AbstractExecutableTest>(capacity);

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
