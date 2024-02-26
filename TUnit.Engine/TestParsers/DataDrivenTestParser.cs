using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.TestParsers;

internal class DataDrivenTestParser(DataSourceRetriever dataSourceRetriever) : ITestParser
{
    public IEnumerable<TestDetails> GetTestCases(MethodInfo methodInfo, 
        Type type, 
        int runCount)
    {
        var dataDrivenTestAttributes = methodInfo.GetCustomAttributes<DataDrivenTestAttribute>().ToList();
        
        if (!dataDrivenTestAttributes.Any())
        {
            yield break;
        }
        
        var count = 0;
        
        foreach (var dataDrivenTestAttribute in dataDrivenTestAttributes)
        {
            var arguments = dataDrivenTestAttribute.Values;
                    
            foreach (var classArguments in dataSourceRetriever.GetTestDataSourceArguments(type))
            {
                for (var i = 1; i <= runCount; i++)
                {
                    yield return new TestDetails(
                        methodInfo: methodInfo,
                        classType: type,
                        methodArguments: arguments,
                        classArguments: DataSourceDrivenTestParser.GetDataSourceArguments(classArguments),
                        count: count++
                    );
                }
            }
        }
    }
}