using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class FailedTestInitializationWriter
{
    public static void GenerateFailedTestCode(SourceCodeWriter sourceBuilder,
        TestSourceDataModel testSourceDataModel)
    {
        var testId = testSourceDataModel.TestId;
        
        sourceBuilder.Write($"nodes.Add(new FailedTestMetadata<{testSourceDataModel.TestClass.GloballyQualified()}>");
        sourceBuilder.Write("{"); 
        sourceBuilder.Write($"TestId = $\"{testId}\",");
        sourceBuilder.Write($"MethodName = $\"{testSourceDataModel.MethodName}\",");
        sourceBuilder.Write($"Exception = new TUnit.Core.Exceptions.TestFailedInitializationException(\"{testSourceDataModel.TestClass.Name}.{testSourceDataModel.MethodName} failed to initialize\", exception),");
        sourceBuilder.Write($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.Write($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.Write("});");
    }
    
    public static void GenerateFailedTestCode(SourceCodeWriter sourceBuilder,
        DynamicTestSourceDataModel testSourceDataModel)
    {
        var testId = $"{testSourceDataModel.Class.GloballyQualified()}{testSourceDataModel.Method.Name}";
        
        sourceBuilder.Write("return");
        sourceBuilder.Write("[");
        sourceBuilder.Write($"new FailedDynamicTest<{testSourceDataModel.Class.GloballyQualified()}>");
        sourceBuilder.Write("{"); 
        sourceBuilder.Write($"TestId = @\"{testId}\",");
        sourceBuilder.Write($"MethodName = $\"{testSourceDataModel.Method.Name}\",");
        sourceBuilder.Write($"Exception = new TUnit.Core.Exceptions.TestFailedInitializationException(\"{testSourceDataModel.Class.Name}.{testSourceDataModel.Method.Name} failed to initialize\", exception),");
        sourceBuilder.Write($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.Write($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.Write("}");
        sourceBuilder.Write("];");
    }
}