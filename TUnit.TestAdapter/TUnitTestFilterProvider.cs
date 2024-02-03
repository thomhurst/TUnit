using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;
using TUnit.TestAdapter.Constants;

namespace TUnit.TestAdapter;

public class TUnitTestFilterProvider(IRunContext runContext, IMessageLogger messageLogger)
{
    public IEnumerable<TestWithTestCase> FilterTests(IEnumerable<TestWithTestCase> tests)
    {
        var filterExpression = runContext.GetTestCaseFilter(null, _ => null);

        messageLogger.SendMessage(TestMessageLevel.Informational, $"TestCaseFilterValue is: {filterExpression?.TestCaseFilterValue}");
        
        if (filterExpression is null)
        {
            foreach (var testWithTestCase in tests)
            {
                yield return testWithTestCase;
            }
            
            yield break;
        }
        
        foreach (var testWithTestCase in tests)
        {
            var (testDetails, _) = testWithTestCase;

            if (string.IsNullOrWhiteSpace(filterExpression.TestCaseFilterValue))
            {
                yield return testWithTestCase;
                continue;
            }
            
            var filter = ParseFilter(filterExpression.TestCaseFilterValue);

            if (TestMatchesFilter(testDetails, filter))
            {
                yield return testWithTestCase;
            }
        }
    }

    private Filter ParseFilter(string testCaseFilterValue)
    {
        var filter = new Filter();
        
        foreach (var filterSegment in testCaseFilterValue.Split(';'))
        {
            var filterSplit = filterSegment.Split('=');
            var filterName = filterSplit.FirstOrDefault();
            var filterValue = filterSplit.ElementAtOrDefault(1);

            if (string.IsNullOrWhiteSpace(filterName) || 
                !TestAdapterConstants.Filters.KnownFilters.Contains(filterName, StringComparer.InvariantCultureIgnoreCase))
            {
                continue;
            }

            filter.AddFilter(filterName, filterValue);
        }

        return filter;
    }

    private bool TestMatchesFilter(TestDetails test, Filter filter)
    {
        messageLogger.SendMessage(TestMessageLevel.Informational, test.ToString());

        if (filter.IsEmpty)
        {
            return true;
        }
        
        if (filter.BannedCategories.Intersect(test.Categories).Any())
        {
            return false;
        }

        return AllowedTestName(test, filter)
            && AllowedCategory(test, filter)
            && AllowedClass(test, filter);
    }

    private static bool AllowedTestName(TestDetails test, Filter filter)
    {
        return !filter.RunnableTestNames.Any() ||
               filter.RunnableTestNames.Contains(test.SimpleMethodName, StringComparer.InvariantCultureIgnoreCase);
    }
    
    private static bool AllowedCategory(TestDetails test, Filter filter)
    {
        return !filter.RunnableCategories.Any() ||
               filter.RunnableCategories.Intersect(test.Categories, StringComparer.InvariantCultureIgnoreCase).Any();
    }
    
    private static bool AllowedClass(TestDetails test, Filter filter)
    {
        return AllowedSimpleClass(test, filter)
            && AllowedFullyQualifiedClass(test, filter);
    }
    
    private static bool AllowedSimpleClass(TestDetails test, Filter filter)
    {
        return !filter.RunnableClasses.Any() ||
               filter.RunnableClasses.Contains(test.ClassType.Name, StringComparer.InvariantCultureIgnoreCase);
    }
    
    private static bool AllowedFullyQualifiedClass(TestDetails test, Filter filter)
    {
        return !filter.RunnableFullyQualifiedClasses.Any() ||
               filter.RunnableFullyQualifiedClasses.Contains(test.ClassType.FullName, StringComparer.InvariantCultureIgnoreCase);
    }
}