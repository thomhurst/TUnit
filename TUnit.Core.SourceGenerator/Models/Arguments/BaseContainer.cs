using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public abstract record BaseContainer
{
    public HashSet<Variable> DataAttributesVariables { get; } = [];
    public HashSet<Variable> DataVariables { get; } = [];
    public abstract void OpenScope(SourceCodeWriter sourceCodeWriter, ref int variableIndex);
    public abstract AttributeData? Attribute { get; init; }
    public abstract void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex);
    public abstract void CloseScope(SourceCodeWriter sourceCodeWriter);
    public abstract string[] GetArgumentTypes();
    
    public virtual void BeforeWriteTestNode(SourceCodeWriter sourceCodeWriter)
    {
    }
}