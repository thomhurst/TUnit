using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TUnit.TestAdapter;

internal class TestMethodRetriever
{
    public MethodInfo GetTestMethod(Type classType, TestCase testCase)
    {
        var matchingMethodNames = classType.GetMethods()
            .Where(x => x.Name == testCase.GetPropertyValue(TUnitTestProperties.TestName, ""))
            .ToList();

        if (matchingMethodNames.Count == 1)
        {
            return matchingMethodNames.First();
        }

        var testParameterTypeNames =
            testCase.GetPropertyValue(TUnitTestProperties.MethodParameterTypeNames, Array.Empty<string>());

        return matchingMethodNames.First(x => x.GetParameters()
            .Select(p => p.ParameterType.FullName)
            .SequenceEqual(testParameterTypeNames)
        );
    }
}