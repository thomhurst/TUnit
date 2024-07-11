using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;
using TUnit.Engine.SourceGenerator.Models.Arguments;

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
        
        var methodParameterTypesList = string.Join(", ", testSourceDataModel.MethodParameterTypes.Select(x => $"typeof({x})"));

        sourceBuilder.WriteLine(
            $"var methodInfo = typeof({fullyQualifiedClassType}).GetMethod(\"{testSourceDataModel.MethodName}\", {testSourceDataModel.MethodGenericTypeCount}, [{methodParameterTypesList}]);");
        
        if (hasEnumerableClassData)
        {
            sourceBuilder.WriteLine($"foreach (var {VariableNames.ClassData} in {testSourceDataModel.ClassArguments.SafeFirstOrDefault()?.Invocation})");
            sourceBuilder.WriteLine("{");
            sourceBuilder.WriteLine($"{VariableNames.EnumerableClassDataIndex}++;");
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
            $"var resettableClassFactory = new ResettableLazy<{fullyQualifiedClassType}>(() => new {testSourceDataModel.FullyQualifiedTypeName}({testSourceDataModel.GetClassArgumentVariableNamesAsList()}));");

        sourceBuilder.WriteLine();
        
        if (hasEnumerableMethodData)
        {
            sourceBuilder.WriteLine($"foreach (var {VariableNames.MethodData} in {testSourceDataModel.MethodArguments.SafeFirstOrDefault()?.Invocation})");
            sourceBuilder.WriteLine("{");
            sourceBuilder.WriteLine($"{VariableNames.EnumerableTestDataIndex}++;");
        }
        
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

        sourceBuilder.WriteLine($"TestRegistrar.RegisterTest<{fullyQualifiedClassType}>(new TestMetadata<{fullyQualifiedClassType}>");
        sourceBuilder.WriteLine("{"); 
        sourceBuilder.WriteLine($"TestId = $\"{testId}\",");
        sourceBuilder.WriteLine($"TestClassArguments = [{testSourceDataModel.GetClassArgumentVariableNamesAsList()}],");
        sourceBuilder.WriteLine($"TestMethodArguments = [{testSourceDataModel.GetCommaSeparatedMethodArgumentVariableNames()}],");
        sourceBuilder.WriteLine($"InternalTestClassArguments = [{string.Join(", ", testSourceDataModel.ClassArguments.Select((a, i) => ToInjectedType(a, i, VariableNames.ClassArg)))}],");
        sourceBuilder.WriteLine($"InternalTestMethodArguments = [{string.Join(", ", testSourceDataModel.MethodArguments.Select((a, i) => ToInjectedType(a, i, VariableNames.MethodArg)))}],");
        sourceBuilder.WriteLine($"CurrentRepeatAttempt = {testSourceDataModel.CurrentRepeatAttempt},");
        sourceBuilder.WriteLine($"RepeatLimit = {testSourceDataModel.RepeatLimit},");
        sourceBuilder.WriteLine("MethodInfo = methodInfo,");
        sourceBuilder.WriteLine("ResettableClassFactory = resettableClassFactory,");
        sourceBuilder.WriteLine($"BeforeEachTestSetUps = [{testSourceDataModel.BeforeEachTestInvocations}],");
        sourceBuilder.WriteLine($"TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.{testSourceDataModel.MethodName}({testSourceDataModel.GetCommaSeparatedMethodArgumentVariableNamesWithCancellationToken()})),");
        sourceBuilder.WriteLine($"AfterEachTestCleanUps = [{testSourceDataModel.AfterEachTestInvocations}],");
        sourceBuilder.WriteLine($"DisplayName = $\"{GetDisplayName(testSourceDataModel)}\",");
        sourceBuilder.WriteLine($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.WriteLine($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.WriteLine("});");
        if (hasEnumerableClassData)
        {
            sourceBuilder.WriteLine("}");
        }
        
        if (hasEnumerableMethodData)
        {
            sourceBuilder.WriteLine(
                $"resettableClassFactory = new ResettableLazy<{fullyQualifiedClassType}>(() => new {testSourceDataModel.FullyQualifiedTypeName}({testSourceDataModel.GetClassArgumentVariableNamesAsList()}));");

            sourceBuilder.WriteLine("}");
        }
    }

    private static string ToInjectedType(Argument arg, int index, string variablePrefix)
    {
        if (arg is KeyedSharedArgument keyedSharedArgument)
        {
            return $$"""
                   new TestData({{variablePrefix}}{{index}}, typeof({{arg.Type}}), InjectedDataType.SharedByKey)
                   {
                        StringKey = "{{keyedSharedArgument.Key}}"
                   }
                   """;
        }

        if (arg is GloballySharedArgument)
        {
            return $"new TestData({variablePrefix}{index}, typeof({arg.Type}), InjectedDataType.SharedGlobally)";
        }

        if (arg is TestClassTypeSharedArgument)
        {
            return $"new TestData({variablePrefix}{index}, typeof({arg.Type}), InjectedDataType.SharedByTestClassType)";
        }

        return $$"""
                new TestData({{variablePrefix}}{{index}}, typeof({{arg.Type}}), InjectedDataType.None)
                				{
                    				DisposeAfterTest = {{arg.DisposeAfterTest.ToString().ToLower()}},
                				}
                """;
    }

    private static string GetDisplayName(TestSourceDataModel testSourceDataModel)
    {
        var customDisplayName = GetCustomDisplayName(testSourceDataModel);

        if (!string.IsNullOrEmpty(customDisplayName))
        {
            return customDisplayName!;
        }

        return $"{testSourceDataModel.MethodName}{GetMethodArgs(testSourceDataModel)}";
    }

    private static string? GetCustomDisplayName(TestSourceDataModel testSourceDataModel)
    {
        var displayName = testSourceDataModel.CustomDisplayName;

        if (string.IsNullOrEmpty(displayName))
        {
            return null;
        }
        
        var args = testSourceDataModel.GetMethodArgumentVariableNames().ToList();

        for (var index = 0; index < testSourceDataModel.MethodParameterNames.Length; index++)
        {
            var methodParameterName = testSourceDataModel.MethodParameterNames[index];
            displayName = displayName!.Replace($"${methodParameterName}", $"{{{args[index]}}}");
        }

        return displayName;
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