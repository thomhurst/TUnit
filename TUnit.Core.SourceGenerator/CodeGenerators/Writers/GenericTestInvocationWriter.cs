using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class GenericTestInvocationWriter
{
    public static void GenerateTestInvocationCode(ICodeWriter sourceBuilder,
        TestSourceDataModel testSourceDataModel)
    {
        var testId = testSourceDataModel.TestId;

        var fullyQualifiedClassType = testSourceDataModel.FullyQualifiedTypeName;

        sourceBuilder.Append("var testInformation = ");

        SourceInformationWriter.GenerateMethodInformation(sourceBuilder,
            testSourceDataModel.TestGenerationContext.Context,
            testSourceDataModel.ClassMetadata, testSourceDataModel.MethodMetadata,
            testSourceDataModel.GenericSubstitutions, ';');

        sourceBuilder.AppendLine();

        sourceBuilder.Append("var testBuilderContext = new global::TUnit.Core.TestBuilderContext");
        sourceBuilder.Append("{");
        sourceBuilder.Append($"TestMethodName = \"{testSourceDataModel.MethodName}\",");
        sourceBuilder.Append("ClassInformation = testInformation.Class,");
        sourceBuilder.Append("MethodInformation = testInformation");
        sourceBuilder.Append("};");
        sourceBuilder.Append("var testBuilderContextAccessor = new global::TUnit.Core.TestBuilderContextAccessor(testBuilderContext);");

        WriteScopesAndArguments(sourceBuilder, testSourceDataModel);

        foreach (var (propertySymbol, argumentsContainer) in testSourceDataModel.PropertyArguments.InnerContainers.Where(c => c.PropertySymbol.IsStatic))
        {
            sourceBuilder.Append($"{fullyQualifiedClassType}.{propertySymbol.Name} = {argumentsContainer.DataVariables.Select(x => x.Name).ElementAt(0)};");
            sourceBuilder.Append($"global::TUnit.Core.SourceRegistrar.RegisterGlobalInitializer(async () => await global::TUnit.Core.ObjectInitializer.InitializeAsync({fullyQualifiedClassType}.{propertySymbol.Name}));");
        }

        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine();

        sourceBuilder.Append($"testDefinitions.Add(new TestDefinition<{fullyQualifiedClassType}>");
        sourceBuilder.Append("{");
        sourceBuilder.Append($"TestId = $\"{testId}\",");
        sourceBuilder.Append("MethodMetadata = testInformation,");
        sourceBuilder.Append($"TestFilePath = @\"{testSourceDataModel.FilePath}\",");
        sourceBuilder.Append($"TestLineNumber = {testSourceDataModel.LineNumber},");
        sourceBuilder.Append($"TestClassFactory = () => resettableClassFactory.Value,");
        sourceBuilder.Append($"TestMethodInvoker = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.{testSourceDataModel.MethodName}({testSourceDataModel.MethodVariablesWithCancellationToken()})),");
        sourceBuilder.Append($"ClassArgumentsProvider = () => new object?[] {{ {testSourceDataModel.ClassArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()} }},");
        sourceBuilder.Append($"MethodArgumentsProvider = () => new object?[] {{ {testSourceDataModel.MethodArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()} }},");
        sourceBuilder.Append("PropertiesProvider = () => new global::System.Collections.Generic.Dictionary<string, object?>");
        sourceBuilder.Append("{");
        foreach (var propertyContainer in testSourceDataModel.PropertyArguments.InnerContainers.Where(x => !x.PropertySymbol.IsStatic))
        {
            sourceBuilder.Append($"[\"{propertyContainer.PropertySymbol.Name}\"] = {propertyContainer.ArgumentsContainer.DataVariables.Select(variable => variable.Name).ToCommaSeparatedString()},");
        }
        sourceBuilder.Append("}");
        sourceBuilder.Append("});");

        sourceBuilder.Append("resettableClassFactory = resettableClassFactoryDelegate();");
        sourceBuilder.Append("testBuilderContext = new global::TUnit.Core.TestBuilderContext");
        sourceBuilder.Append("{");
        sourceBuilder.Append($"TestMethodName = \"{testSourceDataModel.MethodName}\",");
        sourceBuilder.Append("ClassInformation = testInformation.Class,");
        sourceBuilder.Append("MethodInformation = testInformation");
        sourceBuilder.Append("};");
        sourceBuilder.Append("testBuilderContextAccessor.Current = testBuilderContext;");

        testSourceDataModel.ClassArguments.CloseScope(sourceBuilder);
        testSourceDataModel.MethodArguments.CloseScope(sourceBuilder);
    }

    private static void WriteScopesAndArguments(ICodeWriter sourceBuilder, TestSourceDataModel testSourceDataModel)
    {
        var methodVariablesIndex = 0;
        var classVariablesIndex = 0;
        var propertiesVariablesIndex = 0;

        sourceBuilder.Append($"{testSourceDataModel.ClassMetadata.GloballyQualified()}? classInstance = null;");;
        sourceBuilder.Append("object?[]? classInstanceArguments = null;");;

        if (NeedsClassInstantiatedForMethodData(testSourceDataModel))
        {
            // Instance method data sources need to access the class, so we need to tweak the ordering of how they're generated
            // We don't want to do this always because the standard ordering is better at reducing data re-use or leakage between tests
            testSourceDataModel.ClassArguments.OpenScope(sourceBuilder, ref classVariablesIndex);
            testSourceDataModel.ClassArguments.WriteVariableAssignments(sourceBuilder, ref classVariablesIndex);

            sourceBuilder.Append($"classInstanceArguments = [{testSourceDataModel.ClassArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()}];");

            testSourceDataModel.PropertyArguments.WriteVariableAssignments(sourceBuilder, ref propertiesVariablesIndex);

            NewClassWriter.ConstructClass(sourceBuilder, testSourceDataModel.FullyQualifiedTypeName, testSourceDataModel.ClassArguments, testSourceDataModel.PropertyArguments);

            sourceBuilder.Append(
                "var resettableClassFactory = resettableClassFactoryDelegate();");

            sourceBuilder.Append("classInstance = resettableClassFactory.Value;");

            testSourceDataModel.MethodArguments.OpenScope(sourceBuilder, ref methodVariablesIndex);

            testSourceDataModel.MethodArguments.WriteVariableAssignments(sourceBuilder, ref methodVariablesIndex);

            return;
        }

        testSourceDataModel.ClassArguments.OpenScope(sourceBuilder, ref classVariablesIndex);
        testSourceDataModel.MethodArguments.OpenScope(sourceBuilder, ref methodVariablesIndex);

        testSourceDataModel.ClassArguments.WriteVariableAssignments(sourceBuilder, ref classVariablesIndex);

        sourceBuilder.Append($"classInstanceArguments = [{testSourceDataModel.ClassArguments.DataVariables.Select(x => x.Name).ToCommaSeparatedString()}];");

        testSourceDataModel.PropertyArguments.WriteVariableAssignments(sourceBuilder, ref propertiesVariablesIndex);
        testSourceDataModel.MethodArguments.WriteVariableAssignments(sourceBuilder, ref methodVariablesIndex);

        NewClassWriter.ConstructClass(sourceBuilder, testSourceDataModel.FullyQualifiedTypeName, testSourceDataModel.ClassArguments, testSourceDataModel.PropertyArguments);

        sourceBuilder.Append(
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

        if (testSourceDataModel.MethodMetadata.Parameters
            .SelectMany(x => x.GetAttributes())
            .SelectMany(x => x.AttributeClass?.Interfaces ?? ImmutableArray<INamedTypeSymbol>.Empty)
            .Any(x => x.GloballyQualified() == "global::TUnit.Core.IAccessesInstanceData"))
        {
            return true;
        }

        return false;
    }
}
