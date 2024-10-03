using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ClassPropertiesContainer(IReadOnlyCollection<(IPropertySymbol PropertySymbol, ArgumentsContainer ArgumentsContainer)> PropertyContainers)
{
    public void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter)
    {
        foreach (var (_, argumentsContainer) in PropertyContainers)
        {
            argumentsContainer.WriteVariableAssignments(sourceCodeWriter);
        }
    }

    public void WriteObjectInitializer(SourceCodeWriter sourceCodeWriter)
    {
        if (!PropertyContainers.Any())
        {
            return;
        }
        
        sourceCodeWriter.WriteLine("{");
        
        foreach (var (propertySymbol, argumentsContainer) in PropertyContainers)
        {
            sourceCodeWriter.WriteLine($"{propertySymbol.Name} = {argumentsContainer.VariableNames[0]},");
        }

        sourceCodeWriter.WriteLine("}");
    }
}