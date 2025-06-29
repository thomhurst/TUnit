using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public abstract record BaseContainer
{
    public HashSet<Variable> DataAttributesVariables { get; } = [];
    public HashSet<Variable> DataVariables { get; } = [];
    public abstract void OpenScope(ICodeWriter sourceCodeWriter, ref int variableIndex);
    public abstract AttributeData? Attribute { get; init; }
    public abstract void WriteVariableAssignments(ICodeWriter sourceCodeWriter, ref int variableIndex);
    public abstract void CloseScope(ICodeWriter sourceCodeWriter);
    public abstract string[] GetArgumentTypes();

    public virtual void BeforeWriteTestNode(ICodeWriter sourceCodeWriter)
    {
    }
}
