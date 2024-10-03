using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ClassDataSourceAttributeContainer : DataAttributeContainer
{
    public ClassDataSourceAttributeContainer(ArgumentsType ArgumentsType) : base(ArgumentsType)
    {
        VariableNames = [GenerateUniqueVariableName()];
    }

    public required string TypeName { get; init; }
    public required string SharedArgumentType { get; init; }
    
    public required string? ForClass { get; init; }
    public required string? Key { get; init; }
    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter)
    {
        var variableName = VariableNames[0];
        
        if (SharedArgumentType is "TUnit.Core.SharedType.Globally")
        {
            sourceCodeWriter.WriteLine(
                $"{TypeName} {variableName} = TestDataContainer.GetGlobalInstance<{TypeName}>(() => new {TypeName}());");
        }

        else if (SharedArgumentType is "TUnit.Core.SharedType.ForClass")
        {
            sourceCodeWriter.WriteLine(
                $"{TypeName} {variableName} = TestDataContainer.GetInstanceForType<{TypeName}>(typeof({ForClass}), () => new {TypeName}());");
        }

        else if (SharedArgumentType is "TUnit.Core.SharedType.Keyed")
        {
            sourceCodeWriter.WriteLine(
                $"{TypeName} {variableName} = TestDataContainer.GetInstanceForKey<{TypeName}>(\"{Key}\", () => new {TypeName}());");
        }

        else
        {
            sourceCodeWriter.WriteLine($"{TypeName} {variableName} = new {TypeName}();");
        }
        
        sourceCodeWriter.WriteLine();
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        // Nothing
    }

    public override string[] VariableNames { get; }

    public override string[] GetArgumentTypes()
    {
        return [TypeName];
    }
}