using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class FailedTestInitializationWriter
{
    public static void GenerateFailedTestCode(SourceCodeWriter sourceBuilder,
        TestSourceDataModel testSourceDataModel)
    {
        var testId = testSourceDataModel.TestId;
        
        sourceBuilder.WriteLine($"nodes.Add(new FailedTestMetadata<{testSourceDataModel.TestClass.GloballyQualified()}>");
        sourceBuilder.WriteLine("{"); 
        sourceBuilder.WriteLine($"TestId = $\"{testId}\",");
        sourceBuilder.WriteLine($"MethodName = $\"{testSourceDataModel.MethodName}\",");
        sourceBuilder.WriteLine($"Exception = new TUnit.Core.Exceptions.TestFailedInitializationException(\"{testSourceDataModel.TestClass.Name}.{testSourceDataModel.MethodName} failed to initialize\", exception),");
        sourceBuilder.WriteLine($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.WriteLine($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.WriteLine("});");
    }
    
    public static void GenerateFailedTestCode(SourceCodeWriter sourceBuilder,
        DynamicTestSourceDataModel testSourceDataModel)
    {
        var testId = $"{testSourceDataModel.Class.GloballyQualified()}{testSourceDataModel.Method.Name}";
        
        sourceBuilder.WriteLine("return");
        sourceBuilder.WriteLine("[");
        sourceBuilder.WriteLine($"new FailedDynamicTest<{testSourceDataModel.Class.GloballyQualified()}>");
        sourceBuilder.WriteLine("{"); 
        sourceBuilder.WriteLine($"TestId = @\"{testId}\",");
        sourceBuilder.WriteLine($"MethodName = $\"{testSourceDataModel.Method.Name}\",");
        sourceBuilder.WriteLine($"Exception = new TUnit.Core.Exceptions.TestFailedInitializationException(\"{testSourceDataModel.Class.Name}.{testSourceDataModel.Method.Name} failed to initialize\", exception),");
        sourceBuilder.WriteLine($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.WriteLine($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.WriteLine("}");
        sourceBuilder.WriteLine("];");
    }
}