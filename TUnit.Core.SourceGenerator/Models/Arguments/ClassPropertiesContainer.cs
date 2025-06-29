using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record ClassPropertiesContainer(
    IReadOnlyCollection<(IPropertySymbol PropertySymbol, ArgumentsContainer ArgumentsContainer)> InnerContainers)
    : ArgumentsContainer(ArgumentsType.Property)
{
    public override void OpenScope(ICodeWriter sourceCodeWriter, ref int variableIndex)
    {
    }

    public override void WriteVariableAssignments(ICodeWriter sourceCodeWriter, ref int variableIndex)
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

        sourceCodeWriter.AppendLine();
    }

    public override void CloseScope(ICodeWriter sourceCodeWriter)
    {
    }

    public override string[] GetArgumentTypes()
    {
        return InnerContainers.SelectMany(x => x.ArgumentsContainer.GetArgumentTypes()).ToArray();
    }

    public void WriteObjectInitializer(ICodeWriter sourceCodeWriter)
    {
        if (!InnerContainers.Any())
        {
            return;
        }

        sourceCodeWriter.Append("{");

        foreach (var (propertySymbol, argumentsContainer) in InnerContainers.Where(x => !x.PropertySymbol.IsStatic))
        {
            var firstElement = argumentsContainer.DataVariables.ElementAt(0).Name;
            sourceCodeWriter.Append($"{propertySymbol.Name} = global::TUnit.Core.Helpers.CastHelper.Cast<{propertySymbol.Type.GloballyQualified()}>({firstElement}),");
        }

        sourceCodeWriter.Append("}");
    }
}
