using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record GeneratedArgumentsContainer(
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
        
        sourceCodeWriter.WriteLine($$"""
                                     var {{dataGeneratorMetadataVariableName}} = new DataGeneratorMetadata
                                     {
                                        Type = global::TUnit.Core.Enums.DataGeneratorType.{{type}},
                                        TestBuilderContext = testBuilderContextAccessor,
                                        TestInformation = testInformation,
                                        MembersToGenerate = {{SourceInformationWriter.GenerateMembers(Context, Parameters, null, ArgumentsType)}},
                                        TestSessionId = sessionId,
                                     };
                                     """);
        
        var arrayVariableName = $"{VariableNamePrefix}GeneratedDataArray";
        var generatedDataVariableName = $"{VariableNamePrefix}GeneratedData";

        var dataAttr = GenerateDataAttributeVariable("var", AttributeWriter.WriteAttribute(Context, AttributeData),
            ref variableIndex);
            
        sourceCodeWriter.WriteLine(dataAttr.ToString());
        
        sourceCodeWriter.WriteLine();
        
        sourceCodeWriter.WriteLine($"var {arrayVariableName} = {dataAttr.Name}.GenerateDataSources({dataGeneratorMetadataVariableName});");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine($"foreach (var {generatedDataVariableName}Accessor in {arrayVariableName})");
        sourceCodeWriter.WriteLine("{");

        if (ArgumentsType == ArgumentsType.ClassConstructor)
        {
            sourceCodeWriter.WriteLine($"{CodeGenerators.VariableNames.ClassDataIndex}++;");
        }
        
        if (ArgumentsType == ArgumentsType.Method)
        {
            sourceCodeWriter.WriteLine($"{CodeGenerators.VariableNames.TestMethodDataIndex}++;");
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
                AttributeWriter.WriteAttribute(Context, AttributeData),
                ref variableIndex);
            
            sourceCodeWriter.WriteLine(attr.ToString());
            
            sourceCodeWriter.WriteLine(GenerateVariable("var", $$"""
                                                                 {{attr.Name}}.GenerateDataSources(new DataGeneratorMetadata
                                                                 {
                                                                    Type = global::TUnit.Core.Enums.DataGeneratorType.{{type}},
                                                                    TestBuilderContext = testBuilderContextAccessor,
                                                                    TestInformation = testInformation,
                                                                    MembersToGenerate = {{SourceInformationWriter.GenerateMembers(Context, ImmutableArray<IParameterSymbol>.Empty, Property, ArgumentsType.Property)}},
                                                                    TestSessionId = sessionId,
                                                                 }).ElementAtOrDefault(0)()
                                                                 """, ref variableIndex).ToString());
            sourceCodeWriter.WriteLine();
            return;
        }
        
        var generatedDataVariableName = $"{VariableNamePrefix}GeneratedData";
        
        sourceCodeWriter.WriteLine($"var {generatedDataVariableName} = {generatedDataVariableName}Accessor();");

        if (GenericArguments.Length == 0)
        {
            for (var i = 0; i < ParameterOrPropertyTypes.Length; i++)
            {
                var refIndex = i;
                
                if (IsStronglyTyped)
                {
                    sourceCodeWriter.WriteLine(GenerateVariable(ParameterOrPropertyTypes[i].GloballyQualified(), $"({ParameterOrPropertyTypes[i].GloballyQualified()}){generatedDataVariableName}[{i}]", ref refIndex).ToString());
                }
                else
                {
                    sourceCodeWriter.WriteLine(GenerateVariable(ParameterOrPropertyTypes[i].GloballyQualified(), $"global::TUnit.Core.Helpers.CastHelper.Cast<{ParameterOrPropertyTypes[i].GloballyQualified()}>({generatedDataVariableName}[{i}])", ref refIndex).ToString());
                }
            }
        }
        else if (GenericArguments.Length > 1)
        {
            for (var i = 0; i < GenericArguments.Length; i++)
            {
                var refIndex = i;
                sourceCodeWriter.WriteLine(GenerateVariable(GenericArguments[i], $"{generatedDataVariableName}.Item{i + 1}", ref refIndex).ToString());
            }

            sourceCodeWriter.WriteLine();
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

    public override void CloseScope(SourceCodeWriter sourceCodeWriter)
    {
        if (ArgumentsType is ArgumentsType.Property)
        {
            return;
        }
        
        sourceCodeWriter.WriteLine("}");
    }
    
    public override string[] GetArgumentTypes()
    {
        return GenericArguments;
    }
}