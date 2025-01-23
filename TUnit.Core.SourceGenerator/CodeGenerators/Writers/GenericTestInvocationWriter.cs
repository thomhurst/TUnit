using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class GenericTestInvocationWriter
{
    public static void GenerateTestInvocationCode(SourceProductionContext context, SourceCodeWriter sourceBuilder,
        TestSourceDataModel testSourceDataModel)
    {
        var testId = testSourceDataModel.TestId;

        var fullyQualifiedClassType = testSourceDataModel.FullyQualifiedTypeName;
        
        sourceBuilder.WriteLine($"var testClassType = typeof({fullyQualifiedClassType});");
        
        sourceBuilder.WriteLine($"var methodInfo = {MethodInfoWriter.Write(testSourceDataModel)};");
        
        sourceBuilder.WriteLine();
        
        sourceBuilder.WriteLine("var testBuilderContext = new global::TUnit.Core.TestBuilderContext();");
        sourceBuilder.WriteLine("var testBuilderContextAccessor = new global::TUnit.Core.TestBuilderContextAccessor(testBuilderContext);");

        var methodVariablesIndex = 0;
        var classVariablesIndex = 0;
        var propertiesVariablesIndex = 0;

        if (testSourceDataModel.ClassArguments is GeneratedArgumentsContainer
            || testSourceDataModel.MethodArguments is GeneratedArgumentsContainer
            || testSourceDataModel.PropertyArguments.InnerContainers.Any(x =>
                x.ArgumentsContainer is GeneratedArgumentsContainer))
        {
            sourceBuilder.WriteLine($"var classInformation = {SourceInformationWriter.GenerateClassInformation(testSourceDataModel.TestGenerationContext.Context, testSourceDataModel.TestClass)};");

            sourceBuilder.WriteLine($"var testInformation = {SourceInformationWriter.GenerateTestInformation(testSourceDataModel.TestGenerationContext.Context, testSourceDataModel.TestMethod)};");
        }

        testSourceDataModel.ClassArguments.OpenScope(sourceBuilder, ref classVariablesIndex);
        testSourceDataModel.PropertyArguments.OpenScope(sourceBuilder, ref propertiesVariablesIndex);
        testSourceDataModel.MethodArguments.OpenScope(sourceBuilder, ref methodVariablesIndex);

        testSourceDataModel.ClassArguments.WriteVariableAssignments(sourceBuilder, ref classVariablesIndex);
        testSourceDataModel.PropertyArguments.WriteVariableAssignments(sourceBuilder, ref propertiesVariablesIndex);
        testSourceDataModel.MethodArguments.WriteVariableAssignments(sourceBuilder, ref methodVariablesIndex);

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
        
        sourceBuilder.WriteLine($"nodes.Add(new TestMetadata<{fullyQualifiedClassType}>");
        sourceBuilder.WriteLine("{"); 
        sourceBuilder.WriteLine($"TestId = $\"{testId}\",");
        sourceBuilder.WriteLine($"TestClassArguments = [{testSourceDataModel.ClassArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"TestMethodArguments = [{testSourceDataModel.MethodArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"TestClassProperties = [{testSourceDataModel.PropertyArguments.InnerContainers.Where(x => !x.PropertySymbol.IsStatic).SelectMany(x => x.ArgumentsContainer.DataVariables.Select(variable => variable.Name)).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"CurrentRepeatAttempt = {testSourceDataModel.CurrentRepeatAttempt},");
        sourceBuilder.WriteLine($"RepeatLimit = {testSourceDataModel.RepeatLimit},");
        sourceBuilder.WriteLine("MethodInfo = methodInfo,");
        sourceBuilder.WriteLine("ResettableClassFactory = resettableClassFactory,");
        sourceBuilder.WriteLine($"TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.{testSourceDataModel.MethodName}({testSourceDataModel.MethodVariablesWithCancellationToken()})),");
        sourceBuilder.WriteLine($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.WriteLine($"TestLineNumber = {testSourceDataModel.LineNumber},");
        
        sourceBuilder.WriteLine($"TestAttributes = [ {testSourceDataModel.TestAttributes.ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine($"ClassAttributes = [ {testSourceDataModel.ClassAttributes.ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine($"AssemblyAttributes = [ {testSourceDataModel.AssemblyAttributes.ToCommaSeparatedString()} ],");

        sourceBuilder.WriteLine($"DataAttributes = [ {dataContainers.SelectMany(x => x.DataAttributesVariables).Select(x => x.Name).ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine("TestBuilderContext = testBuilderContext,");
        sourceBuilder.WriteLine("});");
        
        sourceBuilder.WriteLine("resettableClassFactory = resettableClassFactoryDelegate();");
        sourceBuilder.WriteLine("testBuilderContext = new();");
        sourceBuilder.WriteLine("testBuilderContextAccessor.Current = testBuilderContext;");
        
        testSourceDataModel.ClassArguments.CloseScope(sourceBuilder);
        testSourceDataModel.MethodArguments.CloseScope(sourceBuilder);
    }
}