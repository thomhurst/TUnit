using TUnit.Core.SourceGenerator.Enums;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public abstract record ArgumentsContainer(ArgumentsType ArgumentsType) : DataAttributeContainer(ArgumentsType)
{
    public required bool DisposeAfterTest { get; init; }

    protected string VariableNamePrefix
    {
        get
        {
            return ArgumentsType switch
            {
                ArgumentsType.ClassConstructor => CodeGenerators.VariableNames.ClassArg,
                ArgumentsType.Property => CodeGenerators.VariableNames.PropertyArg,
                _ => CodeGenerators.VariableNames.MethodArg
            };
        }
    }
    
    protected string DataAttributeVariableNamePrefix
    {
        get
        {
            return ArgumentsType switch
            {
                ArgumentsType.ClassConstructor => CodeGenerators.VariableNames.ClassDataAttribute,
                ArgumentsType.Property => CodeGenerators.VariableNames.PropertyDataAttribute,
                _ => CodeGenerators.VariableNames.MethodDataAttribute
            };
        }
    }

    protected Variable GenerateVariable(string type, string value, ref int globalIndex)
    {
        if (globalIndex == 0)
        {
            var generateVariable = AddVariable(new Variable
            {
                Type = type,
                Name = VariableNamePrefix,
                Value = value
            });
            
            globalIndex++;
            
            return generateVariable;
        }

        return AddVariable(new Variable
        {
            Type = type,
            Name = $"{VariableNamePrefix}{globalIndex++}",
            Value = value
        });
    }
    
    protected Variable GenerateDataAttributeVariable(string type, string value, ref int globalIndex)
    {
        if (globalIndex == 0)
        {
            globalIndex++;
            return AddDataAttributeVariable(new Variable
            {
                Type = type,
                Name = DataAttributeVariableNamePrefix,
                Value = value
            });
        }

        return AddDataAttributeVariable(new Variable
        {
            Type = type,
            Name = $"{DataAttributeVariableNamePrefix}{globalIndex}",
            Value = value
        });
    }

    protected Variable AddVariable(Variable variable)
    {
        DataVariables.Add(variable);
        return variable;
    }
    
    protected Variable AddDataAttributeVariable(Variable variable)
    {
        DataAttributesVariables.Add(variable);
        return variable;
    }
};