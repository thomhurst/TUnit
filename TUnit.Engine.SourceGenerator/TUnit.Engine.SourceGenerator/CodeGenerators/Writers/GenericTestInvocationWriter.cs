using System;
using System.Linq;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Writers;

internal static class GenericTestInvocationWriter
{
    private const int DefaultOrder = int.MaxValue / 2;

    public static void GenerateTestInvocationCode(SourceCodeWriter sourceBuilder,
        TestSourceDataModel testSourceDataModel)
    {
        var testId = testSourceDataModel.TestId;

        var fullyQualifiedClassType = testSourceDataModel.FullyQualifiedTypeName;

        var hasEnumerableClassData = testSourceDataModel.IsEnumerableClassArguments;
        var hasEnumerableMethodData = testSourceDataModel.IsEnumerableMethodArguments;
        
        var methodParameterTypesList = string.Join(", ", testSourceDataModel.MethodParameterTypes.Select(x => $"typeof({x})"));
        var classParameterTypesList = string.Join(", ", testSourceDataModel.ClassParameterTypes.Select(x => $"typeof({x})"));
        sourceBuilder.WriteLine(
            $"var methodInfo = typeof({fullyQualifiedClassType}).GetMethod(\"{testSourceDataModel.MethodName}\", {testSourceDataModel.MethodGenericTypeCount}, [{methodParameterTypesList}]);");

        sourceBuilder.WriteLine($"var attributes = methodInfo.GetCustomAttributes().Concat(typeof({fullyQualifiedClassType}).GetCustomAttributes()).ToArray();");
        sourceBuilder.WriteLine();

        if (hasEnumerableClassData)
        {
            sourceBuilder.WriteLine($"foreach (var {VariableNames.ClassData} in {testSourceDataModel.ClassArguments.SafeFirstOrDefault()?.Invocation})");
            sourceBuilder.WriteLine("{");
            sourceBuilder.WriteLine($"{VariableNames.EnumerableClassDataIndex}++;");
        }
        
        if (hasEnumerableMethodData)
        {
            sourceBuilder.WriteLine($"foreach (var {VariableNames.MethodData} in {testSourceDataModel.MethodArguments.SafeFirstOrDefault()?.Invocation})");
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
        sourceBuilder.WriteLine("Categories = attributes.OfType<global::TUnit.Core.CategoryAttribute>().Select(x => x.Category).ToArray(),");
        sourceBuilder.WriteLine("LazyClassInstance = resettableClassFactory,");
        sourceBuilder.WriteLine($"ClassType = typeof({fullyQualifiedClassType}),");
        sourceBuilder.WriteLine($"Timeout = {GetAttribute(WellKnownFullyQualifiedClassNames.TimeoutAttribute)}?.Timeout,");
        sourceBuilder.WriteLine("TestAndClassAttributes = attributes,");
        sourceBuilder.WriteLine($"TestClassArguments = [{testSourceDataModel.GetClassArgumentVariableNamesAsList()}],");
        sourceBuilder.WriteLine($"TestMethodArguments = [{testSourceDataModel.GetMethodArgumentVariableNamesAsList()}],");
        sourceBuilder.WriteLine(
            $"TestClassParameterTypes = [{classParameterTypesList}],");
        sourceBuilder.WriteLine(
            $"TestMethodParameterTypes = [{methodParameterTypesList}],");
        sourceBuilder.WriteLine(
            $"NotInParallelConstraintKeys = {GetAttribute(WellKnownFullyQualifiedClassNames.NotInParallelAttribute)}?.ConstraintKeys,");
        sourceBuilder.WriteLine($"RepeatIndex = {testSourceDataModel.RepeatIndex},");
        sourceBuilder.WriteLine($"RepeatCount = {testSourceDataModel.RepeatCount},");
        sourceBuilder.WriteLine($"RetryCount = {GetAttribute(WellKnownFullyQualifiedClassNames.RetryAttribute)}?.Times ?? 0,");
        sourceBuilder.WriteLine("MethodInfo = methodInfo,");
        sourceBuilder.WriteLine($"TestName = \"{testSourceDataModel.MethodName}\",");
        sourceBuilder.WriteLine($"DisplayName = $\"{GetDisplayName(testSourceDataModel)}\",");
        sourceBuilder.WriteLine("CustomProperties = attributes.OfType<global::TUnit.Core.PropertyAttribute>().ToDictionary(x => x.Name, x => x.Value),");

        sourceBuilder.WriteLine($"MethodRepeatCount = {testSourceDataModel.CurrentMethodRepeatCount},");
        sourceBuilder.WriteLine($"ClassRepeatCount = {testSourceDataModel.CurrentClassRepeatCount},");

        sourceBuilder.WriteLine($"ReturnType = {testSourceDataModel.ReturnType},");

        sourceBuilder.WriteLine($"Order = {GetAttribute(WellKnownFullyQualifiedClassNames.NotInParallelAttribute)}?.Order ?? {DefaultOrder},");

        sourceBuilder.WriteLine($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.WriteLine($"TestLineNumber = {testSourceDataModel.LineNumber},");

        sourceBuilder.WriteLine("};");
        sourceBuilder.WriteLine();
        sourceBuilder.WriteLine("var testContext = new global::TUnit.Core.TestContext(testInformation);");
        sourceBuilder.WriteLine();
        sourceBuilder.WriteLine($"global::TUnit.Engine.Hooks.ClassHookOrchestrator.RegisterTestContext(typeof({fullyQualifiedClassType}), testContext);");
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
    }

    private static string GetAttribute(FullyQualifiedTypeName attributeFullyQualifiedName)
    {
        return $"global::TUnit.Engine.Helpers.AttributeHelper.GetAttribute<{attributeFullyQualifiedName.WithGlobalPrefix}>(attributes)";
    }

    private static string GetDisplayName(TestSourceDataModel testSourceDataModel)
    {
        return $"{testSourceDataModel.CustomDisplayName ?? testSourceDataModel.MethodName}{GetMethodArgs(testSourceDataModel)}";
    }

    private static string GetMethodArgs(TestSourceDataModel testSourceDataModel)
    {
        if (!testSourceDataModel.MethodArguments.Any())
        {
            return string.Empty;
        }

        if (testSourceDataModel is { IsEnumerableMethodArguments: true, IsMethodTupleArguments: false })
        {
            return $"({{{VariableNames.MethodData}}})";
        }

        var isMethodTupleArguments = testSourceDataModel.IsMethodTupleArguments;
        var args = testSourceDataModel.GetMethodArgumentVariableNames()
            .Select(x => $"{{{x}}}")
            .Skip(isMethodTupleArguments ? 1 : 0);

        if (isMethodTupleArguments)
        {
            args = args.First()
                .TrimStart('{', '(')
                .TrimEnd('}', ')')
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Select(x => $"{{{x}}}");
        }
        
        return $"({string.Join(", ", args)})";
    }
}