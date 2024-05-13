using System.Linq;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Writers;

internal static class GenericTestInvocationWriter
{
    public static void GenerateTestInvocationCode(SourceCodeWriter sourceBuilder,
        TestSourceDataModel testSourceDataModel)
    {
        var testId = testSourceDataModel.TestId;

        var fullyQualifiedClassType = testSourceDataModel.FullyQualifiedTypeName;

        var hasEnumerableClassData = testSourceDataModel.IsEnumerableClassArguments;
        var hasEnumerableMethodData = testSourceDataModel.IsEnumerableMethodArguments;

        sourceBuilder.WriteLine("{");

        if (hasEnumerableClassData)
        {
            sourceBuilder.WriteLine($"foreach (var classData in {testSourceDataModel.ClassArguments.First().Invocation})");
            sourceBuilder.WriteLine("{");
            sourceBuilder.WriteLine($"{VariableNames.EnumerableClassDataIndex}++;");
        }
        
        if (hasEnumerableMethodData)
        {
            sourceBuilder.WriteLine($"foreach (var methodData in {testSourceDataModel.MethodArguments.First().Invocation})");
            sourceBuilder.WriteLine("{");
            sourceBuilder.WriteLine($"{VariableNames.EnumerableTestDataIndex}++;");
        }

        var classArguments = testSourceDataModel.GetClassArgumentsInvocations();
        var wasArgument = false;
        foreach (var classArgument in classArguments)
        {
            wasArgument = true;
            sourceBuilder.WriteLine(classArgument);
        }

        if (wasArgument)
        {
            sourceBuilder.WriteLine();
        }

        sourceBuilder.WriteLine(
            $"var resettableClassFactory = new global::TUnit.Core.ResettableLazy<{fullyQualifiedClassType}>(() => new {testSourceDataModel.FullyQualifiedTypeName}({testSourceDataModel.GetClassArgumentVariableNamesAsList()}));");
        sourceBuilder.WriteLine(
            $"var methodInfo = global::TUnit.Core.Helpers.MethodHelpers.GetMethodInfo(() => resettableClassFactory.Value.{testSourceDataModel.MethodName});");

        wasArgument = false;
        var methodArguments = testSourceDataModel.GetMethodArgumentsInvocations();
        foreach (var methodArgument in methodArguments)
        {
            wasArgument = true;
            sourceBuilder.WriteLine(methodArgument);
        }

        if (wasArgument)
        {
            sourceBuilder.WriteLine();
        }

        sourceBuilder.WriteLine(
            $"var testInformation = new global::TUnit.Core.TestInformation<{fullyQualifiedClassType}>()");
        sourceBuilder.WriteLine("{");
        sourceBuilder.WriteLine($"TestId = $\"{testId}\",");
        sourceBuilder.WriteLine(
            $"Categories = [{testSourceDataModel.Categories}],");
        sourceBuilder.WriteLine("LazyClassInstance = resettableClassFactory,");
        sourceBuilder.WriteLine($"ClassType = typeof({fullyQualifiedClassType}),");
        sourceBuilder.WriteLine($"Timeout = {testSourceDataModel.Timeout},");
        sourceBuilder.WriteLine($"TestClassArguments = [{testSourceDataModel.GetClassArgumentVariableNamesAsList()}],");
        sourceBuilder.WriteLine($"TestMethodArguments = [{testSourceDataModel.GetMethodArgumentVariableNamesAsList()}],");
        sourceBuilder.WriteLine(
            $"TestClassParameterTypes = typeof({fullyQualifiedClassType}).GetConstructors().First().GetParameters().Select(x => x.ParameterType).ToArray(),");
        sourceBuilder.WriteLine(
            "TestMethodParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray(),");
        sourceBuilder.WriteLine(
            $"NotInParallelConstraintKeys = {testSourceDataModel.NotInParallelConstraintKeys},");
        sourceBuilder.WriteLine($"RepeatIndex = {testSourceDataModel.RepeatIndex},");
        sourceBuilder.WriteLine($"RetryCount = {testSourceDataModel.RetryCount},");
        sourceBuilder.WriteLine("MethodInfo = methodInfo,");
        sourceBuilder.WriteLine($"TestName = \"{testSourceDataModel.MethodName}\",");
        sourceBuilder.WriteLine($"DisplayName = \"{testSourceDataModel.MethodName}{GetMethodArgs(testSourceDataModel)}\",");

        foreach (var customProperty in testSourceDataModel.CustomProperties)
        {
            sourceBuilder.WriteLine(customProperty);
        }

        sourceBuilder.WriteLine($"MethodRepeatCount = {testSourceDataModel.CurrentMethodRepeatCount},");
        sourceBuilder.WriteLine($"ClassRepeatCount = {testSourceDataModel.CurrentClassRepeatCount},");

        sourceBuilder.WriteLine($"ReturnType = {testSourceDataModel.ReturnType},");

        sourceBuilder.WriteLine($"Order = {testSourceDataModel.Order},");

        sourceBuilder.WriteLine($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.WriteLine($"TestLineNumber = {testSourceDataModel.LineNumber},");

        sourceBuilder.WriteLine("};");
        sourceBuilder.WriteLine();
        sourceBuilder.WriteLine("var testContext = new global::TUnit.Core.TestContext(testInformation);");
        sourceBuilder.WriteLine();
        sourceBuilder.WriteLine(
            $"var unInvokedTest = new global::TUnit.Core.UnInvokedTest<{fullyQualifiedClassType}>(resettableClassFactory)");
        sourceBuilder.WriteLine("{");
        sourceBuilder.WriteLine($"Id = $\"{testId}\",");
        sourceBuilder.WriteLine("TestContext = testContext,");
        sourceBuilder.WriteLine(
            $"ApplicableTestAttributes = [{testSourceDataModel.ApplicableTestAttributes}],");
        sourceBuilder.WriteLine($"BeforeEachTestSetUps = [{testSourceDataModel.BeforeEachTestInvocations}],");
        sourceBuilder.WriteLine(
            $"TestBody = classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.{testSourceDataModel.MethodName}({testSourceDataModel.GetMethodArgumentVariableNamesAsList()})),");
        sourceBuilder.WriteLine($"AfterEachTestCleanUps = [{testSourceDataModel.AfterEachTestInvocations}],");
        sourceBuilder.WriteLine("};");
        sourceBuilder.WriteLine();
        sourceBuilder.WriteLine($"global::TUnit.Core.TestDictionary.AddTest($\"{testId}\", unInvokedTest);");
        
        if (hasEnumerableClassData)
        {
            sourceBuilder.WriteLine("}");
        }
        
        if (hasEnumerableMethodData)
        {
            sourceBuilder.WriteLine("}");
        }

        
        sourceBuilder.WriteLine("}");
    }

    private static string GetMethodArgs(TestSourceDataModel testSourceDataModel)
    {
        if (!testSourceDataModel.MethodArguments.Any())
        {
            return string.Empty;
        }

        return $"({string.Join(", ",
            testSourceDataModel.MethodArguments
                .Select(x => x.Invocation)
                .Select(x => x.Replace("\"", "\\\""))
        )})";
    }
}