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
        
        var methodParameterTypesList = string.Join(", ", testSourceDataModel.MethodParameterTypes.Select(x => $"typeof({x})"));

        sourceBuilder.WriteLine(
            $"var methodInfo = typeof({fullyQualifiedClassType}).GetMethod(\"{testSourceDataModel.MethodName}\", {testSourceDataModel.MethodGenericTypeCount}, [{methodParameterTypesList}]);");
        
        sourceBuilder.WriteLine();

        var classVariablesIndex = 0;
        var methodVariablesIndex = 0;
        var propertiesVariablesIndex = 0;
        
        testSourceDataModel.ClassArguments.WriteVariableAssignments(sourceBuilder, ref classVariablesIndex);
        
        testSourceDataModel.PropertyArguments.WriteVariableAssignments(sourceBuilder, ref propertiesVariablesIndex);
        
        sourceBuilder.Write($"var resettableClassFactoryDelegate = () => new ResettableLazy<{fullyQualifiedClassType}>(() => ");
        
        NewClassWriter.ConstructClass(sourceBuilder, testSourceDataModel.FullyQualifiedTypeName, testSourceDataModel.ClassArguments, testSourceDataModel.PropertyArguments);
        
        sourceBuilder.Write(");");
        
        sourceBuilder.WriteLine();
        
        sourceBuilder.WriteLine(
            "var resettableClassFactory = resettableClassFactoryDelegate();");

        sourceBuilder.WriteLine();
        
        testSourceDataModel.MethodArguments.WriteVariableAssignments(sourceBuilder, ref methodVariablesIndex);

        sourceBuilder.WriteLine($"TestRegistrar.RegisterTest<{fullyQualifiedClassType}>(new TestMetadata<{fullyQualifiedClassType}>");
        sourceBuilder.WriteLine("{"); 
        sourceBuilder.WriteLine($"TestId = $\"{testId}\",");
        sourceBuilder.WriteLine($"TestClassArguments = [{testSourceDataModel.ClassArguments.VariableNames.ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"TestMethodArguments = [{testSourceDataModel.MethodArguments.VariableNames.ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"TestClassProperties = [{testSourceDataModel.PropertyArguments.PropertyContainers.SelectMany(x => x.ArgumentsContainer.VariableNames).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"InternalTestClassArguments = [{ToInjectedTypes(testSourceDataModel.ClassArguments).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"InternalTestClassProperties = [{testSourceDataModel.PropertyArguments.PropertyContainers.SelectMany(x => ToInjectedTypes(x.ArgumentsContainer)).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"InternalTestMethodArguments = [{ToInjectedTypes(testSourceDataModel.MethodArguments).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"CurrentRepeatAttempt = {testSourceDataModel.CurrentRepeatAttempt},");
        sourceBuilder.WriteLine($"RepeatLimit = {testSourceDataModel.RepeatLimit},");
        sourceBuilder.WriteLine("MethodInfo = methodInfo,");
        sourceBuilder.WriteLine("ResettableClassFactory = resettableClassFactory,");
        sourceBuilder.WriteLine($"TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.{testSourceDataModel.MethodName}({testSourceDataModel.MethodVariablesWithCancellationToken()})),");
        sourceBuilder.WriteLine($"TestExecutor = {GetTestExecutor(testSourceDataModel.TestExecutor)},");
        sourceBuilder.WriteLine($"ClassConstructor = { GetClassConstructor(testSourceDataModel) },");
        sourceBuilder.WriteLine($"ParallelLimit = {GetParallelLimit(testSourceDataModel.ParallelLimit)},");
        sourceBuilder.WriteLine($"DisplayName = $\"{DisplayNameWriter.GetDisplayName(testSourceDataModel)}\",");
        sourceBuilder.WriteLine($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.WriteLine($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.WriteLine($"AttributeTypes = [ {string.Join(", ", testSourceDataModel.AttributeTypes.Select(x => $"typeof({x})"))} ],");
        sourceBuilder.WriteLine("});");
        
        testSourceDataModel.ClassArguments.CloseInvocationStatementsParenthesis(sourceBuilder);
        testSourceDataModel.MethodArguments.CloseInvocationStatementsParenthesis(sourceBuilder);
    }

    private static string GetClassConstructor(TestSourceDataModel testSourceDataModel)
    {
        return testSourceDataModel.ClassArguments is ClassConstructorAttributeContainer ? "classConstructor" : "null";
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

        return $"TUnit.Core.ParallelLimitProvider.GetParallelLimit<{parallelLimit}>()";
    }

    private static IEnumerable<string> ToInjectedTypes(ArgumentsContainer argumentsContainer)
    {
        var types = argumentsContainer.GetArgumentTypes();
        var variableNames = argumentsContainer.VariableNames;
        
        if (argumentsContainer is not ClassDataSourceAttributeContainer classDataSourceAttributeContainer 
            || classDataSourceAttributeContainer.SharedArgumentType is "TUnit.Core.SharedType.None")
        {
            for (var i = 0; i < types.Length; i++)
            {
                var argumentType = types[i];
                var variableName = variableNames[i];
                
                yield return $$"""
                         new TestData({{variableName}}, typeof({{argumentType}}), InjectedDataType.None)
                         				{
                             				DisposeAfterTest = {{argumentsContainer.DisposeAfterTest.ToString().ToLowerInvariant()}},
                         				}
                         """;
            }
        }
        
        else if (classDataSourceAttributeContainer.Key != null)
        {
            yield return $$"""
                   new TestData({{variableNames[0]}}, typeof({{types[0]}}), InjectedDataType.SharedByKey)
                   {
                        StringKey = "{{classDataSourceAttributeContainer.Key}}"
                   }
                   """;
        }

        else if (classDataSourceAttributeContainer.SharedArgumentType == "TUnit.Core.SharedType.Globally")
        {
            yield return $"new TestData({variableNames[0]}, typeof({types[0]}), InjectedDataType.SharedGlobally)";
        }

        else if (classDataSourceAttributeContainer.ForClass != null)
        {
            yield return $"new TestData({variableNames[0]}, typeof({types[0]}), InjectedDataType.SharedByTestClassType)";
        }
    }
}