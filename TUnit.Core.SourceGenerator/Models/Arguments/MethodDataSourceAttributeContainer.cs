using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record MethodDataSourceAttributeContainer(
    Compilation Compilation,
    ArgumentsType ArgumentsType,
    ImmutableArray<ITypeSymbol> TypesToInject,
    bool IsExpandableFunc,
    bool IsExpandableEnumerable,
    bool IsExpandableTuples,
    string TestClassTypeName,
    string TypeName,
    string MethodName,
    bool IsStatic,
    ITypeSymbol MethodReturnType,
    string ArgumentsExpression)
    : ArgumentsContainer(ArgumentsType)
{
    public override void OpenScope(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        if (!IsExpandableEnumerable)
        {
            return;
        }
        
        var enumerableIndexName = ArgumentsType == ArgumentsType.ClassConstructor
            ? CodeGenerators.VariableNames.ClassDataIndex
            : CodeGenerators.VariableNames.TestMethodDataIndex;
        
        var dataName = ArgumentsType == ArgumentsType.ClassConstructor
            ? CodeGenerators.VariableNames.ClassData
            : CodeGenerators.VariableNames.MethodData;
            
        sourceCodeWriter.WriteLine($"foreach (var {dataName}Accessor in {GetMethodInvocation()})");
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine($"{enumerableIndexName}++;");
    }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        if (IsExpandableEnumerable)
        {
            WriteEnumerable(sourceCodeWriter);
        }
        else if (IsExpandableTuples)
        {
            WriteTuples(sourceCodeWriter);
        }
        else
        {
            sourceCodeWriter.WriteLine(GenerateVariable(TypesToInject[0].GloballyQualified(), $"{GetMethodInvocation()}{FuncParenthesis()}", ref variableIndex).ToString());
        }
    }

    private void WriteTuples(SourceCodeWriter sourceCodeWriter)
    {
        var tupleVariableName = $"{VariableNamePrefix}Tuples";
        
        if (ArgumentsType == ArgumentsType.Property)
        {
            tupleVariableName += Guid.NewGuid().ToString("N");
        }

        sourceCodeWriter.WriteLine($"var {tupleVariableName} = global::System.TupleExtensions.ToTuple<{string.Join(", ", TypesToInject.Select(x => x.GloballyQualified()))}>({GetMethodInvocation()}{FuncParenthesis()});");
            
        for (var index = 0; index < TypesToInject.Length; index++)
        {
            WriteTuples(sourceCodeWriter, ref index, tupleVariableName);
        }
    }

    private void WriteTuples(SourceCodeWriter sourceCodeWriter, ref int index, string tupleVariableName)
    {
        var tupleType = TypesToInject[index];
            
        var accessorIndex = index + 1;

        string accessor;
        if (accessorIndex % 8 != 0)
        {
            accessor = $"{tupleVariableName}.Item{accessorIndex}";
        }
        else
        {
            var newIndex = accessorIndex % 8 + 1;
            var restAccessor = string.Join(".", Enumerable.Repeat("Rest", newIndex));
            accessor = $"{tupleVariableName}.{restAccessor}.Item{newIndex}";
        }

        var refIndex = index;
        
        sourceCodeWriter.WriteLine(GenerateVariable(tupleType.GloballyQualified(), accessor, ref refIndex).ToString());
    }

    private void WriteEnumerable(SourceCodeWriter sourceCodeWriter)
    {
        if (ArgumentsType == ArgumentsType.Property)
        {
            throw new Exception("Property Injection is not supported with Enumerable data");
        }
            
        var dataName = ArgumentsType == ArgumentsType.ClassConstructor
            ? CodeGenerators.VariableNames.ClassData
            : CodeGenerators.VariableNames.MethodData;
            
        sourceCodeWriter.WriteLine($"var {dataName} = {dataName}Accessor{FuncParenthesis()};");
            
        if (IsExpandableTuples)
        {
            var tupleVariableName = $"{VariableNamePrefix}Tuples";
            if (ArgumentsType == ArgumentsType.Property)
            {
                tupleVariableName += Guid.NewGuid().ToString("N");
            }
                
            sourceCodeWriter.WriteLine($"var {tupleVariableName} = global::System.TupleExtensions.ToTuple<{string.Join(", ", TypesToInject.Select(x => x.GloballyQualified()))}>({dataName});");

            for (var index = 0; index < TypesToInject.Length; index++)
            {
                WriteTuples(sourceCodeWriter, ref index, tupleVariableName);
            }
        }
        else
        {
            AddVariable(new Variable
            {
                Type = "var", 
                Name = $"{dataName}", 
                Value = $"{GetMethodInvocation()}"
            });
        }
    }

    private string GetMethodInvocation()
    {
        if (IsStatic)
        {
            return $"{TypeName}.{MethodName}({ArgumentsExpression})";
        }
        
        return $"new {TypeName}().{MethodName}({ArgumentsExpression})";
    }

    public override void CloseScope(SourceCodeWriter sourceCodeWriter)
    {
        if (IsExpandableEnumerable)
        { 
            sourceCodeWriter.WriteLine("}");
        }
    }

    public override string[] GetArgumentTypes()
    {
        return TypesToInject.Select(x => x.GloballyQualified()).ToArray();
    }

    private string FuncParenthesis()
    {
        return IsExpandableFunc ? "()" : string.Empty;
    }
}