#pragma warning disable TPEXP

using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

internal class TestFilterService(TUnitFrameworkLogger logger, TestArgumentRegistrationService testArgumentRegistrationService, ICommandLineOptions? commandLineOptions = null)
{
    private string[]? _tagFilters;

    private string[] GetTagFilters()
    {
        if (_tagFilters != null)
        {
            return _tagFilters;
        }

        if (commandLineOptions != null &&
            commandLineOptions.TryGetOptionArgumentList(TagCommandProvider.Tag, out var tagArgs))
        {
            _tagFilters = tagArgs;
        }
        else
        {
            _tagFilters = [];
        }

        return _tagFilters;
    }

    public IReadOnlyCollection<AbstractExecutableTest> FilterTests(ITestExecutionFilter? testExecutionFilter, IReadOnlyCollection<AbstractExecutableTest> testNodes)
    {
        var tagFilters = GetTagFilters();
        var hasTagFilter = tagFilters.Length > 0;

        if (testExecutionFilter is null or NopFilter)
        {
            if (!hasTagFilter)
            {
                logger.LogTrace("No test filter found.");
                return FilterOutExplicitTests(testNodes);
            }

            // Apply only tag filter
            logger.LogTrace($"Applying tag filter: {string.Join(", ", tagFilters)}");
            var tagFiltered = FilterByTags(testNodes, tagFilters);
            return FilterOutExplicitTests(tagFiltered);
        }

        logger.LogTrace($"Test filter is: {testExecutionFilter.GetType().Name}");

        // If we have a tag filter, apply it on top of the execution filter
        var testSource = hasTagFilter ? FilterByTags(testNodes, tagFilters) : testNodes;

        // Pre-allocate capacity to avoid resizing during filtering
        var capacity = testSource is ICollection<AbstractExecutableTest> col ? col.Count : 16;
        var filteredTests = new List<AbstractExecutableTest>(capacity);
        var filteredExplicitTests = new List<AbstractExecutableTest>(capacity / 4); // Estimate ~25% explicit tests

        foreach (var test in testSource)
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

        // Invoke event receivers BEFORE argument registration so that SkipAttribute
        // and other ITestRegisteredEventReceiver implementations can set SkipReason
        // before any potentially expensive data source initialization occurs.
        // This is critical for derived SkipAttribute subclasses (e.g., skip-in-CI attributes)
        // that need to prevent ClassDataSource initialization when the test should be skipped.
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

        // If the test was marked as skipped by an event receiver, skip argument registration entirely.
        // This avoids initializing expensive shared data sources (e.g., WebApplicationFactory)
        // for tests that will never execute.
        if (!string.IsNullOrEmpty(test.Context.SkipReason))
        {
            test.Context.InvalidateDisplayNameCache();
            return;
        }

        try
        {
            await testArgumentRegistrationService.RegisterTestArgumentsAsync(test.Context);
        }
        catch (Exception ex)
        {
            // Mark the test as failed - event receivers have already run above
            test.SetResult(TestState.Failed, ex);
        }

        // Clear the cached display name after registration events
        // This ensures that ArgumentDisplayFormatterAttribute and similar attributes
        // have a chance to register their formatters before the display name is finalized
        test.Context.InvalidateDisplayNameCache();
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
        var classTypeName = GetNestedClassName(classMetadata);

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

        // Pre-calculate capacity: 2 properties per category + tags + custom properties
        var categoryCount = test.Context.Metadata.TestDetails.Categories.Count;
        var tagCount = test.Context.Metadata.TestDetails.Tags.Count;
        var customPropCount = test.Context.Metadata.TestDetails.CustomProperties.Sum(p => p.Value.Count);
        var properties = new List<IProperty>(categoryCount * 2 + tagCount + customPropCount);

        foreach (var category in test.Context.Metadata.TestDetails.Categories)
        {
            properties.Add(new TestMetadataProperty(category));
            properties.Add(new TestMetadataProperty("Category", category));
        }

        foreach (var tag in test.Context.Metadata.TestDetails.Tags)
        {
            properties.Add(new TestMetadataProperty("Tag", tag));
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

    private IReadOnlyCollection<AbstractExecutableTest> FilterByTags(IReadOnlyCollection<AbstractExecutableTest> testNodes, string[] tagFilters)
    {
        var capacity = testNodes is ICollection<AbstractExecutableTest> col ? col.Count : 16;
        var result = new List<AbstractExecutableTest>(capacity);

        foreach (var test in testNodes)
        {
            if (MatchesAnyTag(test, tagFilters))
            {
                result.Add(test);
            }
        }

        return result;
    }

    private static bool MatchesAnyTag(AbstractExecutableTest test, string[] tagFilters)
    {
        var tags = test.Context.Metadata.TestDetails.Tags;

        foreach (var filterTag in tagFilters)
        {
            var matched = false;
            foreach (var testTag in tags)
            {
                if (TestDescriptor.MatchesHierarchicalTag(testTag, filterTag))
                {
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
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

    /// <summary>
    /// Builds the nested class name from ClassMetadata by walking the Parent chain.
    /// Returns names joined with '+' (e.g., "OuterClass+InnerClass").
    /// </summary>
    internal static string GetNestedClassName(ClassMetadata classMetadata)
    {
        if (classMetadata.Parent == null)
        {
            return classMetadata.Name;
        }

        var hierarchy = new List<string>();
        var current = classMetadata;
        while (current != null)
        {
            hierarchy.Add(current.Name);
            current = current.Parent;
        }

        hierarchy.Reverse();
        return string.Join("+", hierarchy);
    }
}
