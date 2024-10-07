using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal abstract record ArgumentsContainer(ArgumentsType ArgumentsType) : DataAttributeContainer(ArgumentsType)
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

    protected string GenerateVariableName(ref int globalIndex)
    {
        if (globalIndex == 0)
        {
            globalIndex++;
            return AddVariable(VariableNamePrefix);
        }
        
        return AddVariable($"{VariableNamePrefix}{globalIndex++}");
    }

    protected string AddVariable(string variableName)
    {
        VariableNames.Add(variableName);
        return variableName;
    }
};