using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

internal class TestsLoader(SourceLocationRetriever sourceLocationRetriever, 
    ClassLoader classLoader, 
    TestDataSourceRetriever testDataSourceRetriever,
    CombinativeSolver combinativeSolver)
{
    private static readonly Type[] TestAttributes = [typeof(TestAttribute), typeof(TestWithDataAttribute), typeof(TestDataSourceAttribute)];

    public IEnumerable<TestDetails> GetTests(TypeInformation typeInformation, Assembly[] allAssemblies)
    {
        var methods = typeInformation.Types.SelectMany(x => x.GetMethods());

        foreach (var methodInfo in methods)
        {
            if (!HasTestAttributes(methodInfo))
            {
                continue;
            }
            
            var sourceLocation = sourceLocationRetriever
                .GetSourceLocation(typeInformation.Assembly.Location, methodInfo.DeclaringType!.FullName!, methodInfo.Name);

            var allClasses = classLoader.GetAllTypes(allAssemblies).ToArray();
            
            var nonAbstractClassesContainingTest = allClasses
                .Where(t => t.IsAssignableTo(methodInfo.DeclaringType!) && !t.IsAbstract)
                .ToArray();

            var repeatCount = methodInfo.CustomAttributes
                .FirstOrDefault(x => x.AttributeType == typeof(RepeatAttribute))
                ?.ConstructorArguments.First().Value as int? ?? 0;

            var runCount = repeatCount + 1;
            
            foreach (var test in CollectTestWithDataAttributeTests(methodInfo, nonAbstractClassesContainingTest, runCount, sourceLocation))
            {
                yield return test;
            }

            foreach (var test in CollectStandardTests(methodInfo, nonAbstractClassesContainingTest, runCount, sourceLocation))
            {
                yield return test;
            }

            foreach (var test in CollectTestDataSourceTests(methodInfo, nonAbstractClassesContainingTest, runCount, sourceLocation))
            {
                yield return test;
            }
        }
    }

    private IEnumerable<TestDetails> CollectTestDataSourceTests(MethodInfo methodInfo,
        Type[] nonAbstractClassesContainingTest, int runCount, SourceLocation sourceLocation)
    {
        var testDataSourceAttributes = methodInfo.GetCustomAttributes<TestDataSourceAttribute>().ToList();
        
        if (!testDataSourceAttributes.Any())
        {
            yield break;
        }
        
        var count = 0;
        
        foreach (var methodArguments in testDataSourceRetriever.GetTestDataSourceArguments(methodInfo))
        {
            foreach (var classType in nonAbstractClassesContainingTest)
            {
                foreach (var classArguments in testDataSourceRetriever.GetTestDataSourceArguments(classType))
                {
                    for (var i = 1; i <= runCount; i++)
                    {
                        yield return new TestDetails(
                            methodInfo: methodInfo,
                            classType: classType,
                            sourceLocation: sourceLocation,
                            methodArguments: methodArguments,
                            classArguments: classArguments,
                            count: count++
                        );
                    }
                }
            }
        }
    }

    private IEnumerable<TestDetails> CollectStandardTests(MethodInfo methodInfo, Type[] nonAbstractClassesContainingTest,
        int runCount, SourceLocation sourceLocation)
    {
        if (!methodInfo.GetCustomAttributes<TestAttribute>().Any())
        {
            yield break;
        }

        var count = 1;
        
        var hasCombinativeAttribute = methodInfo.GetCustomAttribute<CombinativeAttribute>() != null;

        foreach (var classType in nonAbstractClassesContainingTest)
        {
            foreach (var classArguments in testDataSourceRetriever.GetTestDataSourceArguments(classType))
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
                                classArguments: classArguments,
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
                            classArguments: classArguments,
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

    private IEnumerable<TestDetails> CollectTestWithDataAttributeTests(MethodInfo methodInfo,
        Type[] nonAbstractClassesContainingTest, int runCount, SourceLocation sourceLocation)
    {
        var testWithDataAttributes = methodInfo.GetCustomAttributes<TestWithDataAttribute>().ToList();
        
        if (!testWithDataAttributes.Any())
        {
            yield break;
        }
        
        var count = 0;
        
        foreach (var testWithDataAttribute in testWithDataAttributes)
        {
            var arguments = testWithDataAttribute.Values;
                    
            foreach (var classType in nonAbstractClassesContainingTest)
            {
                foreach (var classArguments in testDataSourceRetriever.GetTestDataSourceArguments(classType))
                {
                    for (var i = 1; i <= runCount; i++)
                    {
                        yield return new TestDetails(
                            methodInfo: methodInfo,
                            classType: classType,
                            sourceLocation: sourceLocation,
                            methodArguments: arguments,
                            classArguments: classArguments,
                            count: count++
                        );
                    }
                }
            }
        }
    }

    private static bool HasTestAttributes(MethodInfo methodInfo)
    {
        return methodInfo.CustomAttributes
            .Select(x => x.AttributeType)
            .Intersect(TestAttributes)
            .Any();
    }
}