using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record ClassPropertiesContainer(
    IReadOnlyCollection<(IPropertySymbol PropertySymbol, ArgumentsContainer ArgumentsContainer)> InnerContainers)
    : ArgumentsContainer(ArgumentsType.Property)
{
    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        foreach (var (_, argumentsContainer) in InnerContainers)
        {
            argumentsContainer.WriteVariableAssignments(sourceCodeWriter, ref variableIndex);
        }

        foreach (var (_, argumentsContainer) in InnerContainers)
        {
            foreach (var variableName in argumentsContainer.DataVariables)
            {
                DataVariables.Add(variableName);
            }
        }

        sourceCodeWriter.WriteLine();
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
    }

    public override string[] GetArgumentTypes()
    {
        return InnerContainers.SelectMany(x => x.ArgumentsContainer.GetArgumentTypes()).ToArray();
    }

    public void WriteObjectInitializer(SourceCodeWriter sourceCodeWriter)
    {
        if (!InnerContainers.Any())
        {
            return;
        }
        
        sourceCodeWriter.WriteLine("{");
        
        foreach (var (propertySymbol, argumentsContainer) in InnerContainers.Where(x => !x.PropertySymbol.IsStatic))
        {
            sourceCodeWriter.WriteLine($"{propertySymbol.Name} = {argumentsContainer.DataVariables.Select(x => x.Name).ElementAt(0)},");
        }

        sourceCodeWriter.WriteLine("}");
    }
}