using System.Reflection;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Engine.Extensions;
using TUnit.Engine.Models.Properties;

namespace TUnit.Engine;

internal class TestMethodRetriever
{
    public MethodInfo GetTestMethod(Type classType, TestNode testNode)
    {
        var matchingMethodNames = classType.GetMethods()
            .Where(x => x.Name == testNode.GetRequiredProperty<TestInformationProperty>().TestName)
            .ToList();

        if (matchingMethodNames.Count == 1)
        {
            return matchingMethodNames.First();
        }

        var testParameterTypeNames =
            testNode.GetProperty<MethodParameterTypesProperty>()?.FullyQualifiedTypeNames
            ?? [];

        return matchingMethodNames.First(x => x.GetParameters()
            .Select(p => p.ParameterType.FullName)
            .SequenceEqual(testParameterTypeNames)
        );
    }
}