using System.Reflection;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

internal class TestMethodRetriever
{
    public MethodInfo GetTestMethod(Type classType, TestNode testNode)
    {
        var matchingMethodNames = classType.GetMethods()
            .Where(x => x.Name == testNode.GetPropertyValue(TUnitTestProperties.TestName, ""))
            .ToList();

        if (matchingMethodNames.Count == 1)
        {
            return matchingMethodNames.First();
        }

        var testParameterTypeNames =
            testNode.GetPropertyValue(TUnitTestProperties.MethodParameterTypeNames, Array.Empty<string>());

        return matchingMethodNames.First(x => x.GetParameters()
            .Select(p => p.ParameterType.FullName)
            .SequenceEqual(testParameterTypeNames)
        );
    }
}