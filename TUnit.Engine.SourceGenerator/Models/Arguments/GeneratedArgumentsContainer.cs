using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record GeneratedArgumentsContainer : ArgumentsContainer
{
    public GeneratedArgumentsContainer(ArgumentsType ArgumentsType, int AttributeIndex, string TestClassTypeName, string[] GenericArguments, string AttributeDataGeneratorType) : base(ArgumentsType)
    {
        this.AttributeIndex = AttributeIndex;
        this.TestClassTypeName = TestClassTypeName;
        this.GenericArguments = GenericArguments;
        this.AttributeDataGeneratorType = AttributeDataGeneratorType;
    }
    
    public required string? PropertyName { get; init; }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        if(!VariableNames.Any())
        {
            GenerateArgumentVariableNames(ref variableIndex);
        }

        var objectToGetAttributesFrom = ArgumentsType switch
        {
            ArgumentsType.Method => "methodInfo",
            ArgumentsType.Property => $"typeof({TestClassTypeName}).GetProperty(\"{PropertyName}\")",
            _ => $"typeof({TestClassTypeName})"
        };
        
        var type = ArgumentsType == ArgumentsType.Property ? "Property" : "Parameters";
        
        var parameterInfos = ArgumentsType switch
        {
            ArgumentsType.Property => "null",
            ArgumentsType.ClassConstructor => $"{objectToGetAttributesFrom}.GetConstructors().First().GetParameters()",
            _ => $"{objectToGetAttributesFrom}.GetParameters()"
        };
        
        var propertyInfo = ArgumentsType == ArgumentsType.Property ? objectToGetAttributesFrom : "null";
        
        var dataGeneratorMetadata = $$"""
                                     new DataGeneratorMetadata
                                     {
                                        Type = TUnit.Core.Enums.DataGeneratorType.{{type}},
                                        ParameterInfos = {{parameterInfos}},
                                        PropertyInfo = {{propertyInfo}}
                                     }
                                     """;

        if (ArgumentsType == ArgumentsType.Property)
        {
            sourceCodeWriter.WriteLine($"var {VariableNames.ElementAt(0)} = global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<{AttributeDataGeneratorType}>({objectToGetAttributesFrom}).SelectMany(x => x.GenerateDataSources({dataGeneratorMetadata})).ElementAtOrDefault(0);");
            sourceCodeWriter.WriteLine();
            return;
        }
        
        var arrayVariableName = $"{VariableNamePrefix}GeneratedDataArray";
        var generatedDataVariableName = $"{VariableNamePrefix}GeneratedData";
        
        sourceCodeWriter.WriteLine($"var {arrayVariableName} = global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<{AttributeDataGeneratorType}>({objectToGetAttributesFrom}).SelectMany(x => x.GenerateDataSources({dataGeneratorMetadata}));");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine($"foreach (var {generatedDataVariableName} in {arrayVariableName})");
        sourceCodeWriter.WriteLine("{");

        if (GenericArguments.Length > 1)
        {
            for (var i = 0; i < GenericArguments.Length; i++)
            {
                sourceCodeWriter.WriteLine($"{GenericArguments[i]} {VariableNames.ElementAt(i)} = {generatedDataVariableName}.Item{i + 1};");
            }

            sourceCodeWriter.WriteLine();
        }
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        sourceCodeWriter.WriteLine("}");
    }
    
    public override string[] GetArgumentTypes()
    {
        return GenericArguments;
    }

    public int AttributeIndex { get; }

    public string TestClassTypeName { get; }

    public string[] GenericArguments { get; }

    public string AttributeDataGeneratorType { get; }

    private void GenerateArgumentVariableNames(ref int index)
    {
        if (ArgumentsType == ArgumentsType.Property)
        {
            GenerateVariableName(ref index);
            return;
        }
        
        if (GenericArguments.Length == 1)
        {
            AddVariable($"{VariableNamePrefix}GeneratedData");
            return;
        }

        if (ArgumentsType == ArgumentsType.Property)
        {
            throw new Exception("Multiple data values not supported for property injection.");
        }

        for (var i = 0; i < GenericArguments.Length; i++)
        {
            GenerateVariableName(ref index);
        }
    }
}