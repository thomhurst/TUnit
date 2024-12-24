using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class MethodInfoWriter
{
    public static string Write(TestSourceDataModel testSourceDataModel)
    {
        return $"global::TUnit.Core.Helpers.MethodInfoRetriever.GetMethodInfo(typeof({testSourceDataModel.FullyQualifiedTypeName}), \"{testSourceDataModel.MethodName}\", {testSourceDataModel.MethodGenericTypeCount}, [{string.Join(", ", testSourceDataModel.MethodParameterTypes.Select(x => $"typeof({x})"))}])";
    }
    
    public static string Write(HooksDataModel hooksDataModel)
    {
        return $"global::TUnit.Core.Helpers.MethodInfoRetriever.GetMethodInfo(typeof({hooksDataModel.FullyQualifiedTypeName}), \"{hooksDataModel.MethodName}\", 0, [{string.Join(", ", hooksDataModel.ParameterTypes.Select(x => $"typeof({x})"))}])";
    }
}