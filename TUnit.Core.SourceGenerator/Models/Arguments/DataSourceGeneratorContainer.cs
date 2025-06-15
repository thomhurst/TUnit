using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record DataSourceGeneratorContainer(
    GeneratorAttributeSyntaxContext Context,
    AttributeData AttributeData,
    ArgumentsType ArgumentsType,
    ImmutableArray<ITypeSymbol> ParameterOrPropertyTypes,
    INamedTypeSymbol TestClass,
    IMethodSymbol TestMethod,
    IPropertySymbol? Property,
    ImmutableArray<IParameterSymbol> Parameters,
    string[] GenericArguments) : ArgumentsContainer(ArgumentsType)
{
    public required string? PropertyName { get; init; }
    public required bool IsStronglyTyped { get; init; }

    public override void OpenScope(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        if (ArgumentsType is ArgumentsType.Property)
        {
            // No scope as we don't allow enumerables for properties
            return;
        }

        var type = ArgumentsType == ArgumentsType.Method ? "TestParameters" : "ClassParameters";

        var dataGeneratorMetadataVariableName = $"{VariableNamePrefix}DataGeneratorMetadata";

        sourceCodeWriter.Write($"var {dataGeneratorMetadataVariableName} = new DataGeneratorMetadata");
        sourceCodeWriter.Write("{");
        sourceCodeWriter.Write($"Type = global::TUnit.Core.Enums.DataGeneratorType.{type},");
        sourceCodeWriter.Write("TestBuilderContext = testBuilderContextAccessor,");
        sourceCodeWriter.Write("TestInformation = testInformation,");

        sourceCodeWriter.Write("MembersToGenerate = ");
        SourceInformationWriter.GenerateMembers(sourceCodeWriter, Context, TestClass, Parameters, null, ArgumentsType);

        sourceCodeWriter.Write("TestSessionId = sessionId,");
        sourceCodeWriter.Write("TestClassInstance = classInstance,");
        sourceCodeWriter.Write("ClassInstanceArguments = classInstanceArguments,");
        sourceCodeWriter.Write("};");

        var arrayVariableName = $"{VariableNamePrefix}GeneratedDataArray";
        var generatedDataVariableName = $"{VariableNamePrefix}GeneratedData";

        var dataAttr = GenerateDataAttributeVariable("var", string.Empty,
            ref variableIndex);

        sourceCodeWriter.Write($"{dataAttr.Type} {dataAttr.Name} = ");
        AttributeWriter.WriteAttribute(sourceCodeWriter, Context, AttributeData);
        sourceCodeWriter.Write(";");
        sourceCodeWriter.WriteLine();

        sourceCodeWriter.WriteLine();

        sourceCodeWriter.Write($"testBuilderContext.DataAttributes.Add({dataAttr.Name});");
        sourceCodeWriter.WriteLine();

        sourceCodeWriter.Write($"var {arrayVariableName} = await global::TUnit.Core.Helpers.AsyncDataSourceHelper.ToListAsync(((global::TUnit.Core.IAsyncDataSourceGeneratorAttribute){dataAttr.Name}).GenerateAsync({dataGeneratorMetadataVariableName}));");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.Write($"foreach (var {generatedDataVariableName}Accessor in {arrayVariableName})");
        sourceCodeWriter.Write("{");

        if (ArgumentsType == ArgumentsType.ClassConstructor)
        {
            sourceCodeWriter.Write($"{CodeGenerators.VariableNames.ClassDataIndex}++;");
        }

        if (ArgumentsType == ArgumentsType.Method)
        {
            sourceCodeWriter.Write($"{CodeGenerators.VariableNames.TestMethodDataIndex}++;");
        }
    }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        var type = ArgumentsType switch
        {
            ArgumentsType.Property => "Property",
            ArgumentsType.ClassConstructor => "ClassParameters",
            _ => "TestParameters"
        };

        if (Property is not null)
        {
            var attr = GenerateDataAttributeVariable("var",
                string.Empty,
                ref variableIndex);

            sourceCodeWriter.Write($"{attr.Type} {attr.Name} = ");
            AttributeWriter.WriteAttribute(sourceCodeWriter, Context, AttributeData);
            sourceCodeWriter.Write(";");
            sourceCodeWriter.WriteLine();

            sourceCodeWriter.Write($"testBuilderContext.DataAttributes.Add({attr.Name});");
            sourceCodeWriter.WriteLine();

            // Use the property type instead of var to avoid CS0815
            var propertyType = Property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var dataSourceVariable = GenerateVariable(propertyType, string.Empty, ref variableIndex);

            sourceCodeWriter.Write($"{dataSourceVariable.Type} {dataSourceVariable.Name} = ");

            sourceCodeWriter.Write(GetPropertyAssignmentFromDataSourceGeneratorAttribute(attr.Name, Context, TestClass, Property, sourceCodeWriter.TabLevel, false));
            sourceCodeWriter.WriteLine();
            return;
        }

        var generatedDataVariableName = $"{VariableNamePrefix}GeneratedData";

        sourceCodeWriter.Write($"var {generatedDataVariableName} = await {generatedDataVariableName}Accessor();");

        if (GenericArguments.Length == 0)
        {
            for (var i = 0; i < ParameterOrPropertyTypes.Length; i++)
            {
                var refIndex = i;

                if (IsStronglyTyped)
                {
                    sourceCodeWriter.Write(GenerateVariable(ParameterOrPropertyTypes[i].GloballyQualified(), $"({ParameterOrPropertyTypes[i].GloballyQualified()}){generatedDataVariableName}[{i}]", ref refIndex).ToString());
                }
                else
                {
                    sourceCodeWriter.Write(GenerateVariable(ParameterOrPropertyTypes[i].GloballyQualified(), $"global::TUnit.Core.Helpers.CastHelper.Cast<{ParameterOrPropertyTypes[i].GloballyQualified()}>({generatedDataVariableName}[{i}])", ref refIndex).ToString());
                }
            }
        }
        else if (GenericArguments.Length > 1)
        {
            for (var i = 0; i < GenericArguments.Length; i++)
            {
                var refIndex = i;
                sourceCodeWriter.Write(GenerateVariable(GenericArguments[i], $"global::TUnit.Core.Helpers.CastHelper.Cast<{GenericArguments[i]}>({generatedDataVariableName}[{i}])", ref refIndex).ToString());
            }

            sourceCodeWriter.WriteLine();
        }
        else
        {
            // For single generic argument, extract the first element
            if (GenericArguments.Length == 1)
            {
                var refIndex = 0;
                sourceCodeWriter.Write(GenerateVariable(GenericArguments[0], $"global::TUnit.Core.Helpers.CastHelper.Cast<{GenericArguments[0]}>({generatedDataVariableName}[0])", ref refIndex).ToString());
            }
            else
            {
                DataVariables.Add(new Variable
                {
                    Type = "var",
                    Name = $"{generatedDataVariableName}",
                    Value = string.Empty
                });
            }
        }
    }

    public static string GetPropertyAssignmentFromDataSourceGeneratorAttribute(string attributeVariableName, GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol, IPropertySymbol property, int indentLevel, bool isNested)
    {
        var sourceCodeWriter = new SourceCodeWriter(indentLevel);

        // Note: This is used in attribute object initializers which must be synchronous
        // The framework will initialize these properties asynchronously after creation
        
        // Use null! for non-nullable reference types to indicate framework will initialize
        var isReferenceType = property.Type.IsReferenceType;
        var isNullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated;
        
        if (isReferenceType && !isNullable)
        {
            sourceCodeWriter.Write("null! /* async initialization */");
        }
        else if (property.Type.IsValueType && !isNullable)
        {
            sourceCodeWriter.Write($"default({property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}) /* async initialization */");
        }
        else
        {
            sourceCodeWriter.Write("null /* async initialization */");
        }
        if (isNested)
        {
            sourceCodeWriter.Write(",");
        }
        else
        {
            sourceCodeWriter.Write(";");
        }
        sourceCodeWriter.WriteLine();

        return sourceCodeWriter.ToString();
    }

    public static void WriteDataGeneratorMetadataProperty(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol, IPropertySymbol property)
    {
        sourceCodeWriter.Write("new DataGeneratorMetadata");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.Write("{");
        sourceCodeWriter.Write($"Type = global::TUnit.Core.Enums.DataGeneratorType.Property,");
        sourceCodeWriter.Write("TestBuilderContext = testBuilderContextAccessor,");
        sourceCodeWriter.Write("TestInformation = testInformation,");

        sourceCodeWriter.Write("MembersToGenerate = ");
        SourceInformationWriter.GenerateMembers(sourceCodeWriter, context, namedTypeSymbol, ImmutableArray<IParameterSymbol>.Empty, property, ArgumentsType.Property);

        sourceCodeWriter.Write("TestSessionId = sessionId,");
        sourceCodeWriter.Write("TestClassInstance = classInstance,");
        sourceCodeWriter.Write("ClassInstanceArguments = classInstanceArguments,");
        sourceCodeWriter.Write("}");
    }

    public override void CloseScope(SourceCodeWriter sourceCodeWriter)
    {
        if (ArgumentsType is ArgumentsType.Property)
        {
            return;
        }

        sourceCodeWriter.Write("}");
    }

    public override string[] GetArgumentTypes()
    {
        return GenericArguments;
    }
}
