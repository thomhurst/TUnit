using System.Reflection;
using TUnit.Core;
using TUnit.Engine;

namespace TUnit.TestAdapter;

public class TestsLoader(SourceLocationHelper sourceLocationHelper, ClassLoader classLoader, TestDataSourceRetriever testDataSourceRetriever)
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
            
            var sourceLocation = sourceLocationHelper
                .GetSourceLocation(typeInformation.Assembly.Location, methodInfo.DeclaringType!.FullName!, methodInfo.Name);

            var allClasses = classLoader.GetAllTypes(allAssemblies).ToArray();
            var nonAbstractClassesContainingTest = allClasses
                .Where(t => t.IsAssignableTo(methodInfo.DeclaringType!) && !t.IsAbstract)
                .ToArray();
            
            var count = 0;
            
            foreach (var testWithDataAttribute in methodInfo.CustomAttributes.Where(x => x.AttributeType == typeof(TestWithDataAttribute)))
            {
                count++;
                foreach (var customAttributeTypedArgument in testWithDataAttribute.ConstructorArguments)
                {
                    var arguments =
                        (customAttributeTypedArgument.Value as IEnumerable<CustomAttributeTypedArgument>)
                        ?.Select(x => new ParameterArgument(x.Value?.GetType()!, x.Value))
                        .ToArray();
                    
                    foreach (var classType in nonAbstractClassesContainingTest)
                    {
                        yield return new TestDetails(
                            methodInfo: methodInfo,
                            classType: classType,
                            sourceLocation: sourceLocation,
                            arguments: arguments,
                            count: count
                        );   
                    }
                }
            }
            
            if(methodInfo.CustomAttributes.Any(x => x.AttributeType == typeof(TestAttribute)))
            {
                foreach (var classType in nonAbstractClassesContainingTest)
                {
                    yield return new TestDetails(
                        methodInfo: methodInfo,
                        classType: classType,
                        sourceLocation: sourceLocation,
                        arguments: null,
                        count: 1
                    );
                }
            }
            
            foreach (var testDataSourceAttribute in methodInfo.CustomAttributes.Where(x => x.AttributeType == typeof(TestDataSourceAttribute)))
            {
                count++;
                foreach (var classType in nonAbstractClassesContainingTest)
                {
                    yield return new TestDetails(
                        methodInfo: methodInfo,
                        classType: classType,
                        sourceLocation: sourceLocation,
                        arguments: testDataSourceRetriever.GetTestDataSourceArguments(methodInfo, testDataSourceAttribute, allClasses),
                        count: count
                    );
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