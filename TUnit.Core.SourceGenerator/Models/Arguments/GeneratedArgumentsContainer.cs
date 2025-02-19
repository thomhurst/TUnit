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
        
        sourceCodeWriter.WriteLine($"var {dataGeneratorMetadataVariableName} = new DataGeneratorMetadata");
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine($"Type = global::TUnit.Core.Enums.DataGeneratorType.{type},");
        sourceCodeWriter.WriteLine("TestBuilderContext = testBuilderContextAccessor,");
        sourceCodeWriter.WriteLine("TestInformation = testInformation,");
        
        sourceCodeWriter.WriteTabs();
        sourceCodeWriter.Write("MembersToGenerate = ");
        SourceInformationWriter.GenerateMembers(sourceCodeWriter, Context, Parameters, null, ArgumentsType);
        
        sourceCodeWriter.WriteLine("TestSessionId = sessionId,");
        sourceCodeWriter.WriteLine("};");
        
        var arrayVariableName = $"{VariableNamePrefix}GeneratedDataArray";
        var generatedDataVariableName = $"{VariableNamePrefix}GeneratedData";

        var dataAttr = GenerateDataAttributeVariable("var", string.Empty,
            ref variableIndex);
            
        sourceCodeWriter.WriteTabs();
        sourceCodeWriter.Write($"{dataAttr.Type} {dataAttr.Name} = ");
        AttributeWriter.WriteAttribute(sourceCodeWriter, Context, AttributeData);
        sourceCodeWriter.Write(";");
        sourceCodeWriter.WriteLine();
        
        sourceCodeWriter.WriteLine();

        sourceCodeWriter.WriteLine($"testBuilderContext.DataAttributes.Add({dataAttr.Name});");
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
                string.Empty,
                ref variableIndex);
            
            sourceCodeWriter.WriteTabs();
            sourceCodeWriter.Write($"{attr.Type} {attr.Name} = ");
            AttributeWriter.WriteAttribute(sourceCodeWriter, Context, AttributeData);     
            sourceCodeWriter.Write(";");
            sourceCodeWriter.WriteLine();
            
            sourceCodeWriter.WriteLine($"testBuilderContext.DataAttributes.Add({attr.Name});");
            sourceCodeWriter.WriteLine();
            
            var dataSourceVariable = GenerateVariable("var", string.Empty, ref variableIndex);

            sourceCodeWriter.WriteTabs();
            sourceCodeWriter.Write($"{dataSourceVariable.Type} {dataSourceVariable.Name} = ");
            
            sourceCodeWriter.Write($"{attr.Name}.GenerateDataSources(new DataGeneratorMetadata");
                sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"Type = global::TUnit.Core.Enums.DataGeneratorType.{type},");
                sourceCodeWriter.WriteLine("TestBuilderContext = testBuilderContextAccessor,");
            sourceCodeWriter.WriteLine("TestInformation = testInformation,");
            
            sourceCodeWriter.WriteTabs();
            sourceCodeWriter.Write("MembersToGenerate = ");
            SourceInformationWriter.GenerateMembers(sourceCodeWriter, Context, ImmutableArray<IParameterSymbol>.Empty, Property, ArgumentsType.Property);
                
            sourceCodeWriter.WriteLine("TestSessionId = sessionId,");
            sourceCodeWriter.WriteLine("}).ElementAtOrDefault(0)();");
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