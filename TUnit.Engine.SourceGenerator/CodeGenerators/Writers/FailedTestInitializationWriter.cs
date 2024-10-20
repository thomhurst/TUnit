using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Writers;

internal static class FailedTestInitializationWriter
{
    public static void GenerateFailedTestCode(SourceCodeWriter sourceBuilder,
        TestSourceDataModel testSourceDataModel)
    {
        var testId = testSourceDataModel.TestId;
        
        sourceBuilder.WriteLine($"TestRegistrar.Failed($\"{testId}\", new FailedInitializationTest");
        sourceBuilder.WriteLine("{");
        sourceBuilder.WriteLine($"TestId = $\"{testId}\",");
        sourceBuilder.WriteLine($"TestClass = typeof({testSourceDataModel.FullyQualifiedTypeName}),");
        sourceBuilder.WriteLine($"ReturnType = {MethodInfoWriter.Write(testSourceDataModel, testSourceDataModel.MethodParameterOrArgumentNonGenericTypes.Select(x => $"typeof({x})").ToCommaSeparatedString())}.ReturnType,");
        sourceBuilder.WriteLine($"ParameterTypeFullNames = [{string.Join(", ", testSourceDataModel.MethodParameterOrArgumentNonGenericTypes.Select(x => $"typeof({x})"))}],");
        sourceBuilder.WriteLine($"TestName = \"{testSourceDataModel.MethodName}\",");
        sourceBuilder.WriteLine($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.WriteLine($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.WriteLine("Exception = exception,");
        sourceBuilder.WriteLine("});");
    }
}