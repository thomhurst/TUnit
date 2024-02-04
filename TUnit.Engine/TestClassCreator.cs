using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine;

internal class TestClassCreator(TestDataSourceRetriever testDataSourceRetriever)
{
    public IEnumerable<object?> CreateTestClass(TestDetails testDetails, Type[] allClasses)
    {
        if (testDetails.MethodInfo.IsStatic)
        {
            yield break;
        }
        
        if (testDetails.ClassType.HasAttribute<TestDataSourceAttribute>(out var testDataSourceAttributes))
        {
            foreach (var withTestDataSource in CreateWithTestDataSources(testDetails, testDataSourceAttributes, allClasses))
            {
                yield return withTestDataSource;
            }
        }
        
        yield return CreateBasicClass(testDetails);
    }

    private IEnumerable<object> CreateWithTestDataSources(TestDetails testDetails,
        IEnumerable<TestDataSourceAttribute> testDataSourceAttributes, 
        Type[] allClasses)
    {
        foreach (var testDataSourceAttribute in testDataSourceAttributes)
        {
            var className = testDataSourceAttribute.ClassNameProvidingDataSource;

            ParameterArgument[]? testData;
            if (string.IsNullOrEmpty(className))
            {
                var @class = testDetails.MethodInfo.DeclaringType!;
                
                testData = testDataSourceRetriever.GetTestDataSourceArguments(
                    @class,
                    testDataSourceAttribute.MethodNameProvidingDataSource
                );
            }
            else
            {
                var @class = allClasses.FirstOrDefault(x => x.FullName == className)
                             ?? allClasses.First(x => x.Name == className);

                testData = testDataSourceRetriever.GetTestDataSourceArguments(
                    @class,
                    testDataSourceAttribute.MethodNameProvidingDataSource
                );
            }

            yield return Activator.CreateInstance(
                testDetails.ClassType,
                testData?.Select(x => x.Value).ToArray()
            )!;
        }
    }

    private static object CreateBasicClass(TestDetails testDetails)
    {
        try
        {
            return Activator.CreateInstance(testDetails.MethodInfo.DeclaringType!)!;
        }
        catch (Exception e)
        {
            throw new Exception("Cannot create an instance of the test class. Is there a public parameterless constructor?", e);
        }
    }
}