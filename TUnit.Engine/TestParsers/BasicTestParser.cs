using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.TestParsers;

internal class BasicTestParser(DataSourceRetriever dataSourceRetriever, CombinativeSolver combinativeSolver) : ITestParser
{
    public IEnumerable<TestDetails> GetTestCases(MethodInfo methodInfo, 
        Type type, 
        int runCount,
        SourceLocation sourceLocation)
    {
        if (!methodInfo.GetCustomAttributes<TestAttribute>().Any()
            && !methodInfo.GetCustomAttributes<CombinativeTestAttribute>().Any())
        {
            yield break;
        }

        var count = 1;
        
        var hasCombinativeAttribute = methodInfo.GetCustomAttribute<CombinativeTestAttribute>() != null;

        foreach (var classArguments in dataSourceRetriever.GetTestDataSourceArguments(type))
        {
            for (var i = 1; i <= runCount; i++)
            {
                if (hasCombinativeAttribute)
                {
                    foreach (var combinativeValue in GetCombinativeValues(methodInfo))
                    {
                        yield return new TestDetails(
                            methodInfo: methodInfo,
                            classType: type,
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
                        classType: type,
                        sourceLocation: sourceLocation,
                        methodArguments: null,
                        classArguments: DataSourceDrivenTestParser.GetDataSourceArguments(classArguments),
                        count: count++
                    );
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