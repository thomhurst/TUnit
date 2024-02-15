using System.Diagnostics;
using System.Reflection;
using TUnit.Core;
using TUnit.Engine.Models;
using TUnit.Engine.TestParsers;

namespace TUnit.Engine;

internal class TestsLoader(SourceLocationRetriever sourceLocationRetriever, 
    IEnumerable<ITestParser> testParsers)
{
    private static readonly Type[] TestAttributes = [typeof(TestAttribute), typeof(DataDrivenTestAttribute), typeof(DataSourceDrivenTestAttribute)];

    public IEnumerable<TestDetails> GetTests(TypeInformation typeInformation)
    {
        var nonAbstractClasses = typeInformation.Types.Where(x => !x.IsAbstract);

        foreach (var testMethod in GetTestMethods(nonAbstractClasses))
        {
            var sourceLocation = sourceLocationRetriever
                .GetSourceLocation(typeInformation.Assembly.Location, testMethod.MethodInfo.DeclaringType!.FullName!, testMethod.MethodInfo.Name);

            var repeatCount = testMethod.MethodInfo.GetCustomAttributes<RepeatAttribute>()
                .Concat(testMethod.TestClass.GetCustomAttributes<RepeatAttribute>())
                .FirstOrDefault()
                ?.Times ?? 0;

            var runCount = repeatCount + 1;
            
            var testDetailsEnumerable = testParsers.SelectMany(testParser =>
                testParser.GetTestCases(testMethod.MethodInfo,
                    testMethod.TestClass,
                    runCount,
                    sourceLocation)
            );
            
            foreach (var testDetails in testDetailsEnumerable)
            {
                yield return testDetails;
            }
        }
    }

    private static IEnumerable<TestMethod> GetTestMethods(IEnumerable<Type> nonAbstractClasses)
    {
        foreach (var nonAbstractClass in nonAbstractClasses)
        {
            var methods = nonAbstractClass.GetMethods();

            foreach (var methodInfo in methods)
            {
                if (!HasTestAttributes(methodInfo))
                {
                    continue;
                }

                yield return new TestMethod
                {
                    MethodInfo = methodInfo,
                    TestClass = nonAbstractClass
                };
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