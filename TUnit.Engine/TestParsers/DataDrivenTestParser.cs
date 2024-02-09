using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;

namespace TUnit.Engine.TestParsers;

internal class DataDrivenTestParser(DataSourceRetriever dataSourceRetriever) : ITestParser
{
    public IEnumerable<TestDetails> GetTestCases(MethodInfo methodInfo, 
        Type[] nonAbstractClassesContainingTest, 
        int runCount,
        SourceLocation sourceLocation)
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
                    
            foreach (var classType in nonAbstractClassesContainingTest)
            {
                foreach (var classArguments in dataSourceRetriever.GetTestDataSourceArguments(classType))
                {
                    for (var i = 1; i <= runCount; i++)
                    {
                        yield return new TestDetails(
                            methodInfo: methodInfo,
                            classType: classType,
                            sourceLocation: sourceLocation,
                            methodArguments: arguments,
                            classArguments: DataSourceDrivenTestParser.GetDataSourceArguments(classArguments),
                            count: count++
                        );
                    }
                }
            }
        }
    }
}