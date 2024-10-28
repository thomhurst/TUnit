using TUnit.Core.SourceGenerator.Enums;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record GeneratedArgumentsContainer : ArgumentsContainer
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
        var objectToGetAttributesFrom = ArgumentsType switch
        {
            ArgumentsType.Method => "methodInfo",
            ArgumentsType.Property => $"testClassType.GetProperty(\"{PropertyName}\", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)",
            _ => $"typeof({TestClassTypeName})"
        };
        
        var propertyName = "null";
        if (ArgumentsType == ArgumentsType.Property)
        {
            propertyName = $"propertyInfo{variableIndex}";
            sourceCodeWriter.WriteLine($"var {propertyName} = {objectToGetAttributesFrom};");
            objectToGetAttributesFrom = propertyName;
        }
        
        var type = ArgumentsType == ArgumentsType.Property ? "Property" : "Parameters";
        
        var parameterInfos = ArgumentsType switch
        {
            ArgumentsType.Property => "null",
            ArgumentsType.ClassConstructor => $"{objectToGetAttributesFrom}.GetConstructors().First().GetParameters()",
            _ => $"{objectToGetAttributesFrom}.GetParameters()"
        };
        
        var dataGeneratorMetadata = $$"""
                                     new DataGeneratorMetadata
                                     {
                                        Type = TUnit.Core.Enums.DataGeneratorType.{{type}},
                                        TestClassType = testClassType,
                                        ParameterInfos = {{parameterInfos}},
                                        PropertyInfo = {{propertyName}},
                                        TestObjectBag = objectBag,
                                        TestSessionId = sessionId,
                                     }
                                     """;
        
        if (ArgumentsType == ArgumentsType.Property)
        {
            var attr = GenerateDataAttributeVariable("var",
                $"{objectToGetAttributesFrom}.GetCustomAttributes<{AttributeDataGeneratorType}>(true).ElementAt(0)",
                ref variableIndex);
            
            sourceCodeWriter.WriteLine(attr.ToString());
            
            sourceCodeWriter.WriteLine(GenerateVariable("var", $"{attr.Name}.GenerateDataSources({dataGeneratorMetadata}).ElementAtOrDefault(0)", ref variableIndex).ToString());
            sourceCodeWriter.WriteLine();
            return;
        }
        
        var arrayVariableName = $"{VariableNamePrefix}GeneratedDataArray";
        var generatedDataVariableName = $"{VariableNamePrefix}GeneratedData";

        var dataAttr = GenerateDataAttributeVariable("var",
            $"{objectToGetAttributesFrom}.GetCustomAttributes<{AttributeDataGeneratorType}>(true).ElementAt({AttributeIndex})",
            ref variableIndex);
            
        sourceCodeWriter.WriteLine(dataAttr.ToString());
        
        sourceCodeWriter.WriteLine();
        
        sourceCodeWriter.WriteLine($"var {arrayVariableName} = {dataAttr.Name}.GenerateDataSources({dataGeneratorMetadata});");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine($"foreach (var {generatedDataVariableName} in {arrayVariableName})");
        sourceCodeWriter.WriteLine("{");

        if (ArgumentsType == ArgumentsType.ClassConstructor)
        {
            sourceCodeWriter.WriteLine($"{CodeGenerators.VariableNames.ClassDataIndex}++;");
        }
        
        if (ArgumentsType == ArgumentsType.Method)
        {
            sourceCodeWriter.WriteLine($"{CodeGenerators.VariableNames.TestMethodDataIndex}++;");
        }
        
        if (GenericArguments.Length > 1)
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
                Name = generatedDataVariableName,
                Value = String.Empty
            });
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
    
    public string TestClassTypeName { get; }

    public string[] GenericArguments { get; }

    public string AttributeDataGeneratorType { get; }
}