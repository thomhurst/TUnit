using System.Collections.Immutable;
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
        
        sourceBuilder.WriteLine();
        
        sourceBuilder.WriteLine($"nodes.Add(new TestMetadata<{fullyQualifiedClassType}>");
        sourceBuilder.WriteLine("{"); 
        sourceBuilder.WriteLine($"TestId = $\"{testId}\",");
        sourceBuilder.WriteLine($"TestClassArguments = [{testSourceDataModel.ClassArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()}],");
        sourceBuilder.WriteLine($"TestMethodArguments = [{testSourceDataModel.MethodArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()}],");
        
        sourceBuilder.WriteLine("TestClassProperties = new global::System.Collections.Generic.Dictionary<string, object?>");
        sourceBuilder.WriteLine("{");
        foreach (var propertyContainer in testSourceDataModel.PropertyArguments.InnerContainers.Where(x => !x.PropertySymbol.IsStatic))
        {
            sourceBuilder.WriteLine($"[\"{propertyContainer.PropertySymbol.Name}\"] = {propertyContainer.ArgumentsContainer.DataVariables.Select(variable => variable.Name).ToCommaSeparatedString()},");
        }
        sourceBuilder.WriteLine("},");


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
        sourceBuilder.WriteLine("object?[]? classInstanceArguments = null;");;

        if (NeedsClassInstantiatedForMethodData(testSourceDataModel))
        {
            // Instance method data sources need to access the class, so we need to tweak the ordering of how they're generated
            // We don't want to do this always because the standard ordering is better at reducing data re-use or leakage between tests
            testSourceDataModel.ClassArguments.OpenScope(sourceBuilder, ref classVariablesIndex);
            testSourceDataModel.ClassArguments.WriteVariableAssignments(sourceBuilder, ref classVariablesIndex);
            
            sourceBuilder.WriteLine($"classInstanceArguments = [{testSourceDataModel.ClassArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()}];");
            
            testSourceDataModel.PropertyArguments.WriteVariableAssignments(sourceBuilder, ref propertiesVariablesIndex);

            NewClassWriter.ConstructClass(sourceBuilder, testSourceDataModel.FullyQualifiedTypeName, testSourceDataModel.ClassArguments, testSourceDataModel.PropertyArguments);
            
            sourceBuilder.WriteLine(
                "var resettableClassFactory = resettableClassFactoryDelegate();");
            
            sourceBuilder.WriteLine("classInstance = resettableClassFactory.Value;");
        
            testSourceDataModel.MethodArguments.OpenScope(sourceBuilder, ref methodVariablesIndex);

            testSourceDataModel.MethodArguments.WriteVariableAssignments(sourceBuilder, ref methodVariablesIndex);
            
            return;
        }
        
        testSourceDataModel.ClassArguments.OpenScope(sourceBuilder, ref classVariablesIndex);
        testSourceDataModel.MethodArguments.OpenScope(sourceBuilder, ref methodVariablesIndex);

        testSourceDataModel.ClassArguments.WriteVariableAssignments(sourceBuilder, ref classVariablesIndex);
        
        sourceBuilder.WriteLine($"classInstanceArguments = [{testSourceDataModel.ClassArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()}];");
        
        testSourceDataModel.PropertyArguments.WriteVariableAssignments(sourceBuilder, ref propertiesVariablesIndex);
        testSourceDataModel.MethodArguments.WriteVariableAssignments(sourceBuilder, ref methodVariablesIndex);

        NewClassWriter.ConstructClass(sourceBuilder, testSourceDataModel.FullyQualifiedTypeName, testSourceDataModel.ClassArguments, testSourceDataModel.PropertyArguments);
        
        sourceBuilder.WriteLine(
            "var resettableClassFactory = resettableClassFactoryDelegate();");
    }

    private static bool NeedsClassInstantiatedForMethodData(TestSourceDataModel testSourceDataModel)
    {
        if (testSourceDataModel.MethodArguments.Attribute?.NamedArguments.FirstOrDefault(x => x.Key == "AccessesInstanceData").Value.Value as bool? == true)
        {
            return true;
        }
        
        if (testSourceDataModel.MethodArguments.Attribute?.AttributeClass?.Interfaces.Any(x => x.GloballyQualified() == "global::TUnit.Core.IAccessesInstanceData") is true)
        {
            return true;
        }

        if (testSourceDataModel.TestMethod.Parameters
            .SelectMany(x => x.GetAttributes())
            .SelectMany(x => x.AttributeClass?.Interfaces ?? ImmutableArray<INamedTypeSymbol>.Empty)
            .Any(x => x.GloballyQualified() == "global::TUnit.Core.IAccessesInstanceData"))
        {
            return true;
        }

        return false;
    }
}
