using System.Reflection;
using TUnit.Core;
using TUnit.Engine.TestParsers;

namespace TUnit.Engine;

internal class TestsLoader(SourceLocationRetriever sourceLocationRetriever, 
    ClassLoader classLoader, 
    IEnumerable<ITestParser> testParsers)
{
    private static readonly Type[] TestAttributes = [typeof(TestAttribute), typeof(DataDrivenTestAttribute), typeof(DataSourceDrivenTestAttribute)];

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

            var repeatCount = methodInfo.GetCustomAttributes<RepeatAttribute>()
                .Concat(methodInfo.DeclaringType!.GetCustomAttributes<RepeatAttribute>())
                .FirstOrDefault()
                ?.Times ?? 0;

            var runCount = repeatCount + 1;

            var testDetailsEnumerable = testParsers.SelectMany(testParser =>
                testParser.GetTestCases(methodInfo,
                    nonAbstractClassesContainingTest,
                    runCount,
                    sourceLocation)
            );
            
            foreach (var testDetails in testDetailsEnumerable)
            {
                yield return testDetails;
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