using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class FailedTestInitializationWriter
{
    public static void GenerateFailedTestCode(ICodeWriter sourceBuilder,
        DynamicTestSourceDataModel testSourceDataModel)
    {
        var testId = $"{testSourceDataModel.Class.ContainingNamespace}.{testSourceDataModel.Class.Name}.{testSourceDataModel.Method.Name}_InitializationFailure";

        sourceBuilder.Append("return");
        sourceBuilder.Append("[");
        sourceBuilder.Append($"new global::TUnit.Core.FailedDynamicTest<{testSourceDataModel.Class.GloballyQualified()}>");
        sourceBuilder.Append("{");
        sourceBuilder.Append($"TestId = \"{testId}\",");
        sourceBuilder.Append($"MethodName = $\"{testSourceDataModel.Method.Name}\",");
        sourceBuilder.Append($"Exception = new global::TUnit.Core.Exceptions.TestFailedInitializationException(\"{testSourceDataModel.Class.Name}.{testSourceDataModel.Method.Name} failed to initialize\", exception),");
        sourceBuilder.Append($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.Append($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.Append("}");
        sourceBuilder.Append("];");
    }
}
