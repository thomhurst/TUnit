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
        
        if (filter.RunnableTestNames.Contains(test.SimpleMethodName))
        {
            return true;
        }

        if (filter.RunnableCategories.Intersect(test.Categories).Any())
        {
            return true;
        }

        if (filter.RunnableClasses.Contains(test.ClassType.Name))
        {
            return true;
        }
        
        if (filter.RunnableFullyQualifiedClasses.Contains(test.ClassType.FullName!))
        {
            return true;
        }

        return false;
    }
}