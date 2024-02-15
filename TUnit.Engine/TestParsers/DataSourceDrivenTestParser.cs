using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.TestParsers;

internal class DataSourceDrivenTestParser(DataSourceRetriever dataSourceRetriever) : ITestParser
{
    public IEnumerable<TestDetails> GetTestCases(MethodInfo methodInfo, 
        Type type, 
        int runCount,
        SourceLocation sourceLocation)
    {
        var testDataSourceAttributes = methodInfo.GetCustomAttributes<DataSourceDrivenTestAttribute>().ToList();
        
        if (!testDataSourceAttributes.Any())
        {
            yield break;
        }
        
        var count = 0;
        
        foreach (var methodArguments in dataSourceRetriever.GetTestDataSourceArguments(methodInfo))
        {
            foreach (var classArguments in dataSourceRetriever.GetTestDataSourceArguments(type))
            {
                for (var i = 1; i <= runCount; i++)
                {
                    yield return new TestDetails(
                        methodInfo: methodInfo,
                        classType: type,
                        sourceLocation: sourceLocation,
                        methodArguments: GetDataSourceArguments(methodArguments),
                        classArguments: GetDataSourceArguments(classArguments),
                        count: count++
                    );
                }
            }
        }
    }

    public static object?[]? GetDataSourceArguments(object? methodArguments)
    {
        if (methodArguments is null)
        {
            return null;
        }
        
        return [methodArguments];
    }
}