using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
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
        
        sourceBuilder.WriteTabs();
        sourceBuilder.Write("var testInformation = ");

        SourceInformationWriter.GenerateMethodInformation(sourceBuilder,
            testSourceDataModel.TestGenerationContext.Context,
            testSourceDataModel.TestClass, testSourceDataModel.TestMethod,
            testSourceDataModel.GenericSubstitutions, ';');
        
        sourceBuilder.WriteLine();
        
        sourceBuilder.WriteLine("var testBuilderContext = new global::TUnit.Core.TestBuilderContext();");
        sourceBuilder.WriteLine("var testBuilderContextAccessor = new global::TUnit.Core.TestBuilderContextAccessor(testBuilderContext);");

        WriteScopesAndArguments(sourceBuilder, testSourceDataModel);

        foreach (var (propertySymbol, argumentsContainer) in testSourceDataModel.PropertyArguments.InnerContainers.Where(c => c.PropertySymbol.IsStatic))
        {
            sourceBuilder.WriteLine($"{fullyQualifiedClassType}.{propertySymbol.Name} = {argumentsContainer.DataVariables.Select(x => x.Name).ElementAt(0)};");
        }
        
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
        sourceBuilder.WriteLine("ResettableClassFactory = resettableClassFactory,");
        sourceBuilder.WriteLine($"TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.{testSourceDataModel.MethodName}({testSourceDataModel.MethodVariablesWithCancellationToken()})),");
        sourceBuilder.WriteLine($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.WriteLine($"TestLineNumber = {testSourceDataModel.LineNumber},");
        
        sourceBuilder.WriteLine("TestMethod = testInformation,");
        
        sourceBuilder.WriteLine("TestBuilderContext = testBuilderContext,");
        sourceBuilder.WriteLine("});");
        
        sourceBuilder.WriteLine("resettableClassFactory = resettableClassFactoryDelegate();");
        sourceBuilder.WriteLine("testBuilderContext = new();");
        sourceBuilder.WriteLine("testBuilderContextAccessor.Current = testBuilderContext;");
        
        testSourceDataModel.ClassArguments.CloseScope(sourceBuilder);
        testSourceDataModel.MethodArguments.CloseScope(sourceBuilder);
    }

    private static void WriteScopesAndArguments(SourceCodeWriter sourceBuilder, TestSourceDataModel testSourceDataModel)
    {
        var methodVariablesIndex = 0;
        var classVariablesIndex = 0;
        var propertiesVariablesIndex = 0;
        
        sourceBuilder.WriteLine($"{testSourceDataModel.TestClass.GloballyQualified()}? classInstance = null;");;

        if (NeedsClassInstantiatedForMethodData(testSourceDataModel))
        {
            // Instance method data sources need to access the class, so we need to tweak the ordering of how they're generated
            // We don't want to do this always because the standard ordering is better at reducing data re-use or leakage between tests
            testSourceDataModel.ClassArguments.OpenScope(sourceBuilder, ref classVariablesIndex);
            testSourceDataModel.ClassArguments.WriteVariableAssignments(sourceBuilder, ref classVariablesIndex);
            
            testSourceDataModel.PropertyArguments.WriteVariableAssignments(sourceBuilder, ref propertiesVariablesIndex);

            NewClassWriter.ConstructClass(sourceBuilder, testSourceDataModel.FullyQualifiedTypeName, testSourceDataModel.ClassArguments, testSourceDataModel.PropertyArguments);
        
            testSourceDataModel.MethodArguments.OpenScope(sourceBuilder, ref methodVariablesIndex);

            testSourceDataModel.MethodArguments.WriteVariableAssignments(sourceBuilder, ref methodVariablesIndex);
            
            return;
        }
        
        testSourceDataModel.ClassArguments.OpenScope(sourceBuilder, ref classVariablesIndex);
        testSourceDataModel.MethodArguments.OpenScope(sourceBuilder, ref methodVariablesIndex);

        testSourceDataModel.ClassArguments.WriteVariableAssignments(sourceBuilder, ref classVariablesIndex);
        testSourceDataModel.PropertyArguments.WriteVariableAssignments(sourceBuilder, ref propertiesVariablesIndex);
        testSourceDataModel.MethodArguments.WriteVariableAssignments(sourceBuilder, ref methodVariablesIndex);

        NewClassWriter.ConstructClass(sourceBuilder, testSourceDataModel.FullyQualifiedTypeName, testSourceDataModel.ClassArguments, testSourceDataModel.PropertyArguments);
    }

    private static bool NeedsClassInstantiatedForMethodData(TestSourceDataModel testSourceDataModel)
    {
        if (testSourceDataModel.MethodArguments is MethodDataSourceAttributeContainer { IsStatic: false })
        {
            return true;
        }
        
        if (testSourceDataModel.MethodArguments is DataSourceGeneratorContainer dataSourceGeneratorContainer
            && dataSourceGeneratorContainer.AttributeData.AttributeClass?.GloballyQualified() == "global::TUnit.Core.MatrixDataSourceAttribute"
            && dataSourceGeneratorContainer.TestMethod.Parameters.Any(x => x.GetAttributes().Select(a => a.AttributeClass?.GloballyQualifiedNonGeneric()).Contains("global::TUnit.Core.MatrixMethodAttribute")))
        {
            return true;
        }

        return false;
    }
}
