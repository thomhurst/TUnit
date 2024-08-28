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

        var wasArgument = false;
        foreach (var classArgument in testSourceDataModel.ClassDataInvocations)
        {
            wasArgument = true;
            sourceBuilder.WriteLine(classArgument);
        }

        if (wasArgument)
        {
            sourceBuilder.WriteLine();
        }
        
        sourceBuilder.WriteLine(
            $"var resettableClassFactory = new ResettableLazy<{fullyQualifiedClassType}>(() => new {testSourceDataModel.FullyQualifiedTypeName}({testSourceDataModel.ClassVariables.ToCommaSeparatedString()}));");

        sourceBuilder.WriteLine();
        
        if (hasEnumerableMethodData)
        {
            sourceBuilder.WriteLine($"foreach (var {VariableNames.MethodData} in {testSourceDataModel.MethodArguments.SafeFirstOrDefault()?.Invocation})");
            sourceBuilder.WriteLine("{");
            sourceBuilder.WriteLine($"{VariableNames.EnumerableTestDataIndex}++;");
        }
        
        wasArgument = false;
        foreach (var methodArgument in testSourceDataModel.MethodDataInvocations)
        {
            wasArgument = true;
            sourceBuilder.WriteLine(methodArgument);
        }

        if (wasArgument)
        {
            sourceBuilder.WriteLine();
        }

        var classVariables = testSourceDataModel.ClassVariables;
        var methodVariables = testSourceDataModel.MethodVariables;

        sourceBuilder.WriteLine($"TestRegistrar.RegisterTest<{fullyQualifiedClassType}>(new TestMetadata<{fullyQualifiedClassType}>");
        sourceBuilder.WriteLine("{"); 
        sourceBuilder.WriteLine($"TestId = $\"{testId}\",");
        sourceBuilder.WriteLine($"TestClassArguments = [{classVariables.ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"TestMethodArguments = [{methodVariables.ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"InternalTestClassArguments = [{testSourceDataModel.ClassArguments.Select((a, i) => ToInjectedType(a, i, VariableNames.ClassArg)).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"InternalTestMethodArguments = [{testSourceDataModel.MethodArguments.Select((a, i) => ToInjectedType(a, i, VariableNames.MethodArg)).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"CurrentRepeatAttempt = {testSourceDataModel.CurrentRepeatAttempt},");
        sourceBuilder.WriteLine($"RepeatLimit = {testSourceDataModel.RepeatLimit},");
        sourceBuilder.WriteLine("MethodInfo = methodInfo,");
        sourceBuilder.WriteLine("ResettableClassFactory = resettableClassFactory,");
        sourceBuilder.WriteLine($"TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.{testSourceDataModel.MethodName}({testSourceDataModel.MethodVariablesWithCancellationToken()})),");
        sourceBuilder.WriteLine($"TestExecutor = {GetTestExecutor(testSourceDataModel.TestExecutor)},");
        sourceBuilder.WriteLine($"ParallelLimit = {GetParallelLimit(testSourceDataModel.ParallelLimit)},");
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
                $"resettableClassFactory = new ResettableLazy<{fullyQualifiedClassType}>(() => new {testSourceDataModel.FullyQualifiedTypeName}({testSourceDataModel.ClassVariables.ToCommaSeparatedString()}));");

            sourceBuilder.WriteLine("}");
        }
    }

    private static string GetTestExecutor(string? testExecutor)
    {
        if (string.IsNullOrEmpty(testExecutor))
        {
            return "DefaultExecutor.Instance";
        }

        return $"new {testExecutor}()";
    }
    
    private static string GetParallelLimit(string? parallelLimit)
    {
        if (string.IsNullOrEmpty(parallelLimit))
        {
            return "null";
        }

        return $"TUnit.Engine.Services.ParallelLimitProvider.GetParallelLimit<{parallelLimit}>()";
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
        
        var args = testSourceDataModel.MethodVariables;

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

        var args = testSourceDataModel.MethodVariables.Select(x => $"{{{x}}}");
        
        return $"({string.Join(", ", args)})";
    }
}