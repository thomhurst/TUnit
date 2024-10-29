using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class GenericTestInvocationWriter
{
    public static void GenerateTestInvocationCode(SourceCodeWriter sourceBuilder,
        TestSourceDataModel testSourceDataModel)
    {
        var testId = testSourceDataModel.TestId;

        var fullyQualifiedClassType = testSourceDataModel.FullyQualifiedTypeName;
        
        var methodParameterTypesList = string.Join(", ", testSourceDataModel.MethodParameterOrArgumentNonGenericTypes.Select(x => $"typeof({x})"));
        
        sourceBuilder.WriteLine($"var testClassType = typeof({fullyQualifiedClassType});");
        
        sourceBuilder.WriteLine($"var methodInfo = {MethodInfoWriter.Write(testSourceDataModel, methodParameterTypesList)};");
        
        sourceBuilder.WriteLine();
        
        sourceBuilder.WriteLine("var objectBag = new global::System.Collections.Generic.Dictionary<string, object>();");

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
        
        NewClassWriter.ConstructClass(sourceBuilder, testSourceDataModel.FullyQualifiedTypeName, testSourceDataModel.ClassArguments, testSourceDataModel.PropertyArguments);
        
        sourceBuilder.WriteLine();
        
        sourceBuilder.WriteLine(
            "var resettableClassFactory = resettableClassFactoryDelegate();");

        sourceBuilder.WriteLine();
        
        testSourceDataModel.MethodArguments.WriteVariableAssignments(sourceBuilder, ref methodVariablesIndex);

        sourceBuilder.WriteLine($"nodes.Add(new TestMetadata<{fullyQualifiedClassType}>");
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
        sourceBuilder.WriteLine($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.WriteLine($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.WriteLine($"AttributeTypes = [ {testSourceDataModel.AttributeTypes.Select(x => $"typeof({x})").ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine($"DataAttributes = [ {dataContainers.SelectMany(x => x.DataAttributesVariables).Select(x => x.Name).ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine("ObjectBag = objectBag,");
        sourceBuilder.WriteLine("});");
        
        sourceBuilder.WriteLine(
            "resettableClassFactory = resettableClassFactoryDelegate();");
        
        testSourceDataModel.ClassArguments.CloseInvocationStatementsParenthesis(sourceBuilder);
        testSourceDataModel.MethodArguments.CloseInvocationStatementsParenthesis(sourceBuilder);
    }
}