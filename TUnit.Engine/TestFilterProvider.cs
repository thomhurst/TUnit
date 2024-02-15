using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TUnit.Engine;

internal class TestFilterProvider(IRunContext runContext, IMessageLogger messageLogger)
{
    private static readonly Dictionary<string, TestProperty> SupportedProperties 
        = new(StringComparer.OrdinalIgnoreCase);

    static TestFilterProvider()
    {
        SupportedProperties[nameof(TUnitTestProperties.TestName)] =
            TUnitTestProperties.TestName;
        
        SupportedProperties[nameof(TUnitTestProperties.TestClass)] =
            TUnitTestProperties.TestClass;
        
        SupportedProperties[nameof(TUnitTestProperties.Category)] =
            TUnitTestProperties.Category;
        
        SupportedProperties[nameof(TUnitTestProperties.NotCategory)] =
            TUnitTestProperties.NotCategory;
    }
    
    public IEnumerable<TestCase> FilterTests(IEnumerable<TestCase> tests)
    {
        var filterExpression = runContext.GetTestCaseFilter(SupportedProperties.Keys, 
            propertyName => SupportedProperties.GetValueOrDefault(propertyName));

        messageLogger.SendMessage(TestMessageLevel.Informational, $"TestCaseFilterValue is: {filterExpression?.TestCaseFilterValue}");
        
        if (string.IsNullOrWhiteSpace(filterExpression?.TestCaseFilterValue))
        {
            foreach (var testWithTestCase in tests)
            {
                yield return testWithTestCase;
            }
            
            yield break;
        }
        
        foreach (var test in tests)
        {
            var isMatch = filterExpression.MatchTestCase(test, propertyName =>
            {
                if (SupportedProperties.TryGetValue(propertyName, out var testProperty))
                {
                    return test.GetPropertyValue(testProperty);
                }

                return test.Traits.FirstOrDefault(x => x.Name == propertyName)?.Value;
            });
            
            if (isMatch)
            {
                yield return test;
            }
        }
    }
}