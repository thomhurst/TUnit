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
        
        sourceBuilder.WriteLine($"var testClassType = typeof({fullyQualifiedClassType});");
        
        sourceBuilder.WriteLine(
            $"var methodInfo = testClassType.GetMethod(\"{testSourceDataModel.MethodName}\", {testSourceDataModel.MethodGenericTypeCount}, [{methodParameterTypesList}]);");
        
        sourceBuilder.WriteLine();
        
        sourceBuilder.WriteLine("var objectBag = new global::System.Collections.Generic.Dictionary<string, object?>();");

        var classVariablesIndex = 0;
        var methodVariablesIndex = 0;
        var propertiesVariablesIndex = 0;
        
        testSourceDataModel.ClassArguments.WriteVariableAssignments(sourceBuilder, ref classVariablesIndex);
        
        testSourceDataModel.PropertyArguments.WriteVariableAssignments(sourceBuilder, ref propertiesVariablesIndex);

        foreach (var (propertySymbol, argumentsContainer) in testSourceDataModel.PropertyArguments.InnerContainers)
        {
            if (!propertySymbol.IsStatic)
            {
                continue;
            }
            
            sourceBuilder.WriteLine($"{fullyQualifiedClassType}.{propertySymbol.Name} = {argumentsContainer.DataVariables.Select(x => x.Name).ElementAt(0)};");
        }

        IEnumerable<BaseContainer> dataContainers =
            [
                testSourceDataModel.ClassArguments,
                testSourceDataModel.MethodArguments,
                ..testSourceDataModel.PropertyArguments.InnerContainers.Select(x => x.ArgumentsContainer)
            ];
        
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
        sourceBuilder.WriteLine($"TestClassArguments = [{testSourceDataModel.ClassArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"TestMethodArguments = [{testSourceDataModel.MethodArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"TestClassProperties = [{testSourceDataModel.PropertyArguments.InnerContainers.Where(x => !x.PropertySymbol.IsStatic).SelectMany(x => x.ArgumentsContainer.DataVariables.Select(x => x.Name)).ToCommaSeparatedString()}],");
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
        sourceBuilder.WriteLine($"AttributeTypes = [ {testSourceDataModel.AttributeTypes.Select(x => $"typeof({x})").ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine($"DataAttributes = [ {dataContainers.SelectMany(x => x.DataAttributesVariables).Select(x => x.Name).ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine("ObjectBag = objectBag,");
        sourceBuilder.WriteLine("});");
        
        testSourceDataModel.ClassArguments.CloseInvocationStatementsParenthesis(sourceBuilder);
        testSourceDataModel.MethodArguments.CloseInvocationStatementsParenthesis(sourceBuilder);
    }

    private static IEnumerable<string> GenerateDataAttributes(TestSourceDataModel testSourceDataModel)
    {
        if (testSourceDataModel.ClassArguments is ArgumentsContainer { Attribute.AttributeClass: not null } classArgumentsContainer)
        {
            yield return
                $"testClassType.GetCustomAttributes<{classArgumentsContainer.Attribute.AttributeClass.GloballyQualified()}>().ElementAt({classArgumentsContainer.AttributeIndex})";
        }
        
        if (testSourceDataModel.MethodArguments is ArgumentsContainer { Attribute.AttributeClass: not null } testArgumentsContainer)
        {
            yield return
                $"methodInfo.GetCustomAttributes<{testArgumentsContainer.Attribute.AttributeClass.GloballyQualified()}>().ElementAt({testArgumentsContainer.AttributeIndex})";
        }
        
        foreach (var propertyArgumentsInnerContainer in testSourceDataModel.PropertyArguments.InnerContainers.Where(x => !x.PropertySymbol.IsStatic))
        {
            yield return
                $"testClassType.GetProperty(\"{propertyArgumentsInnerContainer.PropertySymbol.Name}\", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetCustomAttributes<{propertyArgumentsInnerContainer.ArgumentsContainer.Attribute!.AttributeClass!.GloballyQualified()}>().ElementAt(0)";
        }
    }

    private static string GetClassConstructor(TestSourceDataModel testSourceDataModel)
    {
        return testSourceDataModel.ClassArguments is ClassConstructorAttributeContainer classConstructorAttributeContainer ? classConstructorAttributeContainer.DataVariables.ElementAt(0).Name : "null";
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
}