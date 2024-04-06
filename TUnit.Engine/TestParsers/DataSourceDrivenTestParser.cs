using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.TestParsers;

internal class DataSourceDrivenTestParser(DataSourceRetriever dataSourceRetriever) : ITestParser
{
    public IEnumerable<TestDetails> GetTestCases(MethodInfo methodInfo,
        Type type,
        int runCount)
    {
        var testDataSourceAttributes = methodInfo.GetCustomAttributes<DataSourceDrivenTestAttribute>().ToList();

        if (!testDataSourceAttributes.Any())
        {
            yield break;
        }

        var classRepeatCount = 0;

        foreach (var classArguments in dataSourceRetriever.GetTestDataSourceArguments(type))
        {
            classRepeatCount++;
            var methodRepeatCount = 0;

            foreach (var methodArguments in dataSourceRetriever.GetTestDataSourceArguments(methodInfo))
            {
                for (var i = 1; i <= runCount; i++)
                {
                    yield return new TestDetails(
                        methodInfo: methodInfo,
                        classType: type,
                        methodArguments: GetDataSourceArguments(methodArguments),
                        classArguments: GetDataSourceArguments(classArguments),
                        currentClassRepeatCount: classRepeatCount,
                        currentMethodRepeatCount: ++methodRepeatCount
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