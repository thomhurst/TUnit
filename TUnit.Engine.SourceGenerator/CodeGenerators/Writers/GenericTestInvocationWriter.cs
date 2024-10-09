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
            
            sourceBuilder.WriteLine($"{fullyQualifiedClassType}.{propertySymbol.Name} = {argumentsContainer.VariableNames.ElementAt(0)};");
        }

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
        sourceBuilder.WriteLine($"TestClassProperties = [{testSourceDataModel.PropertyArguments.InnerContainers.Where(x => !x.PropertySymbol.IsStatic).SelectMany(x => x.ArgumentsContainer.VariableNames).ToCommaSeparatedString()}],");
        sourceBuilder.WriteTabs();
        sourceBuilder.Write("InternalTestClassArguments = ");
        WriteInternalInjectedTypes(testSourceDataModel.ClassArguments, sourceBuilder);
        sourceBuilder.WriteTabs();
        sourceBuilder.Write("InternalTestClassProperties = ");
        WriteInternalInjectedTypes(testSourceDataModel.PropertyArguments, sourceBuilder);
        sourceBuilder.WriteTabs();
        sourceBuilder.Write("InternalTestMethodArguments = ");
        WriteInternalInjectedTypes(testSourceDataModel.MethodArguments, sourceBuilder);
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
        sourceBuilder.WriteLine($"DataAttributes = [ {string.Join(", ", GenerateDataAttributes(testSourceDataModel))} ],");
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
                $"testClassType.GetProperty(\"{propertyArgumentsInnerContainer.PropertySymbol.Name}\").GetCustomAttributes<{propertyArgumentsInnerContainer.ArgumentsContainer.Attribute!.AttributeClass!.GloballyQualified()}>().ElementAt(0)";
        }
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

    private static void WriteInternalInjectedTypes(BaseContainer container, SourceCodeWriter sourceCodeWriter)
    {
        var variableNames = container.VariableNames;

        if (!variableNames.Any())
        {
            sourceCodeWriter.WriteLine("[],");
            return;
        }
        
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("[");

        WriteInternalArgs(container, sourceCodeWriter, false);
        
        sourceCodeWriter.WriteLine("],");
    }

    private static void WriteInternalArgs(BaseContainer container, SourceCodeWriter sourceCodeWriter, bool isProperties)
    {
        if (container is ClassPropertiesContainer classPropertiesContainer)
        {
            foreach (var (_, argumentsContainer) in classPropertiesContainer.InnerContainers.Where(x => !x.PropertySymbol.IsStatic))
            {
                WriteInternalArgs(argumentsContainer, sourceCodeWriter, true);
            }

            return;
        }

        var variableNames = container.VariableNames;
        var types = container.GetArgumentTypes();
        
        if (container is not ClassDataSourceAttributeContainer classDataSourceAttributeContainer 
            || classDataSourceAttributeContainer.SharedArgumentType is "TUnit.Core.SharedType.None")
        {
            var disposeAfter = (container is not ArgumentsContainer argumentsContainer || argumentsContainer.DisposeAfterTest).ToString().ToLowerInvariant();
            
            for (var i = 0; i < types.Length; i++)
            {
                var argumentType = types[i];
                var variableName = variableNames.ElementAt(i);
                
                if (isProperties)
                {
                    disposeAfter = $"{variableName}DisposeAfter";
                }

                sourceCodeWriter.WriteLine($"new TestData({variableName}, typeof({argumentType}), InjectedDataType.None)");
                sourceCodeWriter.WriteLine("{");
                sourceCodeWriter.WriteLine($"DisposeAfterTest = {disposeAfter},");
                sourceCodeWriter.WriteLine("},");
            }
        }
        
        else if (classDataSourceAttributeContainer.Key != null)
        {
            sourceCodeWriter.WriteLine($"new TestData({variableNames.ElementAt(0)}, typeof({types[0]}), InjectedDataType.SharedByKey)");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"StringKey = \"{classDataSourceAttributeContainer.Key}\"");
            sourceCodeWriter.WriteLine("},");
        }

        else if (classDataSourceAttributeContainer.SharedArgumentType == "TUnit.Core.SharedType.Globally")
        {
            sourceCodeWriter.WriteLine($"new TestData({variableNames.ElementAt(0)}, typeof({types[0]}), InjectedDataType.SharedGlobally),");
        }

        else if (classDataSourceAttributeContainer.ForClass != null)
        {
            sourceCodeWriter.WriteLine($"new TestData({variableNames.ElementAt(0)}, typeof({types[0]}), InjectedDataType.SharedByTestClassType),");
        }
    }
}