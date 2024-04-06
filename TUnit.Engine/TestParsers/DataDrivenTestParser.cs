using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.TestParsers;

internal class DataDrivenTestsParser(DataSourceRetriever dataSourceRetriever) : ITestParser
{
    public IEnumerable<TestDetails> GetTestCases(MethodInfo methodInfo,
        Type type,
        int runCount)
    {
        if (methodInfo.GetCustomAttribute<DataDrivenTestAttribute>() == null)
        {
            yield break;
        }

        var argumentsAttributes = methodInfo.GetCustomAttributes<ArgumentsAttribute>().ToList();

        var classRepeatCount = 0;
        foreach (var classArguments in dataSourceRetriever.GetTestDataSourceArguments(type))
        {
            classRepeatCount++;
            var methodRepeatCount = 0;

            foreach (var argumentsAttribute in argumentsAttributes)
            {
                methodRepeatCount++;
                var arguments = argumentsAttribute.Values;

                for (var i = 1; i <= runCount; i++)
                {
                    yield return new TestDetails(
                        methodInfo: methodInfo,
                        classType: type,
                        methodArguments: arguments,
                        classArguments: DataSourceDrivenTestParser.GetDataSourceArguments(classArguments),
                        currentClassRepeatCount: classRepeatCount,
                        currentMethodRepeatCount: i + methodRepeatCount
                    );
                }
            }
        }
    }
}