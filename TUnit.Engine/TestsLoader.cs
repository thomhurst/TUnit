using System.Reflection;
using TUnit.Core;
using TUnit.Engine.Models;
using TUnit.Engine.TestParsers;

namespace TUnit.Engine;

internal class TestsLoader(IEnumerable<ITestParser> testParsers)
{
    private static readonly Type[] TestAttributes = [typeof(TestAttribute), typeof(ArgumentsAttribute), typeof(DataSourceDrivenTestAttribute), typeof(CombinativeTestAttribute)];

    public IEnumerable<TestDetails> GetTests(CachedAssemblyInformation cachedAssemblyInformation)
    {
        return TestDictionary.GetAllTestDetails()
            .Select(x => new TestDetails(
                x.MethodInfo, 
                x.ClassType,
                x.TestMethodArguments,
                x.TestClassArguments,
                x.ClassRepeatCount,
                x.MethodRepeatCount));
        // var nonAbstractClasses = cachedAssemblyInformation.Types.Where(x => !x.IsAbstract);
        //
        // foreach (var testMethod in GetTestMethods(nonAbstractClasses))
        // {
        //     var repeatCount = testMethod.MethodInfo.GetCustomAttributes<RepeatAttribute>()
        //         .Concat(testMethod.TestClass.GetCustomAttributes<RepeatAttribute>())
        //         .FirstOrDefault()
        //         ?.Times ?? 0;
        //
        //     var runCount = repeatCount + 1;
        //     
        //     var testDetailsEnumerable = testParsers.SelectMany(testParser =>
        //         testParser.GetTestCases(testMethod.MethodInfo,
        //             testMethod.TestClass,
        //             runCount)
        //     );
        //     
        //     foreach (var testDetails in testDetailsEnumerable)
        //     {
        //         yield return testDetails;
        //     }
        // }
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
            .Any(x => TestAttributes.Contains(x.AttributeType));
    }
}