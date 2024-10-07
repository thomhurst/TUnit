using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ClassPropertiesContainer : ArgumentsContainer
{
    public ClassPropertiesContainer(IReadOnlyCollection<(IPropertySymbol PropertySymbol, ArgumentsContainer ArgumentsContainer)> InnerContainers) : base(ArgumentsType.Property)
    {
        this.InnerContainers = InnerContainers;
    }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        foreach (var (_, argumentsContainer) in InnerContainers)
        {
            argumentsContainer.WriteVariableAssignments(sourceCodeWriter, ref variableIndex);
        }

        foreach (var (_, argumentsContainer) in InnerContainers)
        {
            foreach (var variableName in argumentsContainer.VariableNames)
            {
                VariableNames.Add(variableName);
                sourceCodeWriter.WriteLine($"var {variableName}DisposeAfter = {argumentsContainer.DisposeAfterTest.ToString().ToLowerInvariant()};");
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
        
        foreach (var (propertySymbol, argumentsContainer) in InnerContainers)
        {
            sourceCodeWriter.WriteLine($"{propertySymbol.Name} = {argumentsContainer.VariableNames.ElementAt(0)},");
        }

        sourceCodeWriter.WriteLine("}");
    }

    public IReadOnlyCollection<(IPropertySymbol PropertySymbol, ArgumentsContainer ArgumentsContainer)> InnerContainers { get; init; }
}