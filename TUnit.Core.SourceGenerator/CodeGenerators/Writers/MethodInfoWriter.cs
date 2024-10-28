using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class MethodInfoWriter
{
    public static string Write(TestSourceDataModel testSourceDataModel, string methodParameterTypesList)
    {
        if (testSourceDataModel.MethodGenericTypeCount == 0)
        {
            return
                $"typeof({testSourceDataModel.FullyQualifiedTypeName}).GetMethod(\"{testSourceDataModel.MethodName}\", {testSourceDataModel.MethodGenericTypeCount}, [{methodParameterTypesList}])";
        }

        return
            $"""
             typeof({testSourceDataModel.FullyQualifiedTypeName})
                .GetMethods()
                .Where(method => method.IsPublic)
                .Where(method => method.Name == "{testSourceDataModel.MethodName}")
                .Where(method => method.GetParameters().Length == {testSourceDataModel.MethodParameterTypes.Length})
                .Where(method => method.GetGenericArguments().Length == {testSourceDataModel.MethodGenericTypeCount})
                .First()
             """;
    }
}