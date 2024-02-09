using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;

namespace TUnit.Engine.TestParsers;

internal class BasicTestParser(DataSourceRetriever dataSourceRetriever, CombinativeSolver combinativeSolver) : ITestParser
{
    public IEnumerable<TestDetails> GetTestCases(MethodInfo methodInfo, 
        Type[] nonAbstractClassesContainingTest, 
        int runCount,
        SourceLocation sourceLocation)
    {
        if (!methodInfo.GetCustomAttributes<TestAttribute>().Any())
        {
            yield break;
        }

        var count = 1;
        
        var hasCombinativeAttribute = methodInfo.GetCustomAttribute<CombinativeAttribute>() != null;

        foreach (var classType in nonAbstractClassesContainingTest)
        {
            foreach (var classArguments in dataSourceRetriever.GetTestDataSourceArguments(classType))
            {
                for (var i = 1; i <= runCount; i++)
                {
                    if (hasCombinativeAttribute)
                    {
                        foreach (var combinativeValue in GetCombinativeValues(methodInfo))
                        {
                            yield return new TestDetails(
                                methodInfo: methodInfo,
                                classType: classType,
                                sourceLocation: sourceLocation,
                                methodArguments: combinativeValue.ToArray(),
                                classArguments: DataSourceDrivenTestParser.GetDataSourceArguments(classArguments),
                                count: count++
                            );
                        }
                    }
                    else
                    {
                        yield return new TestDetails(
                            methodInfo: methodInfo,
                            classType: classType,
                            sourceLocation: sourceLocation,
                            methodArguments: null,
                            classArguments: DataSourceDrivenTestParser.GetDataSourceArguments(classArguments),
                            count: count++
                        );
                    }
                }
            }
        }
    }
    
    private IEnumerable<IEnumerable<object?>> GetCombinativeValues(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();

        var parametersWithValues = parameters
            .Select(GetCombinativeValues)
            .ToList();

        return combinativeSolver.GetCombinativeArgumentsList(parametersWithValues);
    }
    
    private static object?[] GetCombinativeValues(ParameterInfo parameterInfo)
    {
        return parameterInfo.GetCustomAttribute<CombinativeValuesAttribute>()?.Objects ?? [null];
    }
}