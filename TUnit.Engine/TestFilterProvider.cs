using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;

namespace TUnit.Engine;

internal class TestFilterProvider(IRunContext runContext, ILogger<T> logger)
{
    private static readonly Dictionary<string, TestProperty> SupportedProperties 
        = new(StringComparer.OrdinalIgnoreCase);

    public bool IsFilteredTestRun { get; private set; }
    
    static TestFilterProvider()
    {
        SupportedProperties[nameof(TUnitTestProperties.TestName)] =
            TUnitTestProperties.TestName;
        
        SupportedProperties[nameof(TUnitTestProperties.TestClass)] =
            TUnitTestProperties.TestClass;
        
        SupportedProperties[nameof(TUnitTestProperties.Category)] =
            TUnitTestProperties.Category;
    }
    
    public IEnumerable<TestNode> FilterTests(IEnumerable<TestNode> tests)
    {
        var filterExpression = runContext.GetTestCaseFilter(SupportedProperties.Keys, 
            propertyName => SupportedProperties.GetValueOrDefault(propertyName));

        logger.SendMessage(TestMessageLevel.Informational, $"TestCaseFilterValue is: {filterExpression?.TestCaseFilterValue}");
        
        if (string.IsNullOrWhiteSpace(filterExpression?.TestCaseFilterValue))
        {
            foreach (var testWithTestCase in tests)
            {
                yield return testWithTestCase;
            }
            
            yield break;
        }

        IsFilteredTestRun = true;
        
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