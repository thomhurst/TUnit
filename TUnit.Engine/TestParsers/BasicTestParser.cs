using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.TestParsers;

internal class BasicTestParser(DataSourceRetriever dataSourceRetriever, CombinativeSolver combinativeSolver) : ITestParser
{
    public IEnumerable<TestDetails> GetTestCases(MethodInfo methodInfo, 
        Type type, 
        int runCount)
    {
        if (!methodInfo.GetCustomAttributes<TestAttribute>().Any()
            && !methodInfo.GetCustomAttributes<CombinativeTestAttribute>().Any())
        {
            yield break;
        }
        
        var hasCombinativeAttribute = methodInfo.GetCustomAttribute<CombinativeTestAttribute>() != null;

        var classRepeatCount = 0;
        
        foreach (var classArguments in dataSourceRetriever.GetTestDataSourceArguments(type))
        {
            classRepeatCount++;
            var methodRepeatCount = 1;

            for (var i = 1; i <= runCount; i++)
            {
                if (hasCombinativeAttribute)
                {
                    foreach (var combinativeValue in GetCombinativeValues(methodInfo))
                    {
                        yield return new TestDetails(
                            methodInfo: methodInfo,
                            classType: type,
                            methodArguments: combinativeValue.ToArray(),
                            classArguments: DataSourceDrivenTestParser.GetDataSourceArguments(classArguments),
                            currentClassRepeatCount: classRepeatCount,
                            currentMethodRepeatCount: methodRepeatCount++
                        );
                    }
                }
                else
                {
                    yield return new TestDetails(
                        methodInfo: methodInfo,
                        classType: type,
                        methodArguments: null,
                        classArguments: DataSourceDrivenTestParser.GetDataSourceArguments(classArguments),
                        currentClassRepeatCount: classRepeatCount,
                        currentMethodRepeatCount: methodRepeatCount++
                    );
                }
            }
        }
    }
    
    private IEnumerable<IEnumerable<object?>> GetCombinativeValues(MethodInfo methodInfo)
    {
        ParameterInfo[] parameters = methodInfo.GetParameters();

        if (methodInfo.GetCustomAttribute<TimeoutAttribute>() != null)
        {
            parameters = parameters.Take(parameters.Length - 1).ToArray();
        }
        
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