using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class FailedTestInitializationWriter
{
    public static void GenerateFailedTestCode(ICodeWriter sourceBuilder,
        TestSourceDataModel testSourceDataModel)
    {
        var testId = testSourceDataModel.TestId;

        sourceBuilder.Append($"discoveryFailures.Add(new DiscoveryFailure");
        sourceBuilder.Append("{");
        sourceBuilder.Append($"TestId = $\"{testId}\",");
        sourceBuilder.Append($"Exception = new TUnit.Core.Exceptions.TestFailedInitializationException(\"{testSourceDataModel.ClassMetadata.Name}.{testSourceDataModel.MethodName} failed to initialize\", exception),");
        sourceBuilder.Append($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.Append($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.Append($"TestClassName = \"{testSourceDataModel.ClassMetadata.Name}\",");
        sourceBuilder.Append($"TestMethodName = \"{testSourceDataModel.MethodName}\"");
        sourceBuilder.Append("});");
    }

    public static void GenerateFailedTestCode(ICodeWriter sourceBuilder,
        DynamicTestSourceDataModel testSourceDataModel)
    {
        var testId = $"{testSourceDataModel.Class.GloballyQualified()}{testSourceDataModel.Method.Name}";

        sourceBuilder.Append("return");
        sourceBuilder.Append("[");
        sourceBuilder.Append($"new FailedDynamicTest<{testSourceDataModel.Class.GloballyQualified()}>");
        sourceBuilder.Append("{");
        sourceBuilder.Append($"TestId = @\"{testId}\",");
        sourceBuilder.Append($"MethodName = $\"{testSourceDataModel.Method.Name}\",");
        sourceBuilder.Append($"Exception = new TUnit.Core.Exceptions.TestFailedInitializationException(\"{testSourceDataModel.Class.Name}.{testSourceDataModel.Method.Name} failed to initialize\", exception),");
        sourceBuilder.Append($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.Append($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.Append("}");
        sourceBuilder.Append("];");
    }
}
