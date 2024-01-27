using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace TUnit.TestAdapter;

public class TUnitTestFilterProvider(IRunContext runContext)
{
    private static readonly Dictionary<string,TestProperty> _supportedPropertiesCache;
    private static readonly List<string> SupportedProperties = [];

    static TUnitTestFilterProvider()
    {
        // Initialize the property cache
        _supportedPropertiesCache = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase)
        {
            ["FullyQualifiedName"] = TestCaseProperties.FullyQualifiedName,
            ["Name"] = TestCaseProperties.DisplayName,
            ["TestCategory"] = TUnitProperties.TestCategory,
        };

        SupportedProperties.AddRange(_supportedPropertiesCache.Keys);
    }

    public ITestCaseFilterExpression? GetFilter()
    {
        return runContext.GetTestCaseFilter(SupportedProperties, name =>
            _supportedPropertiesCache.TryGetValue(name, out var property)
                ? property
                : TestProperty.Find(name)
        );
    }

    public IEnumerable<TestWithTestCase> FilterTests(IEnumerable<TestWithTestCase> tests)
    {
        var filter = GetFilter();

        if (filter is null)
        {
            foreach (var testWithTestCase in tests)
            {
                yield return testWithTestCase;
            }
            
            yield break;
        }
        
        foreach (var testWithTestCase in tests)
        {
            var (testDetails, testCase) = testWithTestCase;

            if (filter.MatchTestCase(testCase, TestProperty.Find))
            {
                yield return testWithTestCase;
            }
        }
    }
}