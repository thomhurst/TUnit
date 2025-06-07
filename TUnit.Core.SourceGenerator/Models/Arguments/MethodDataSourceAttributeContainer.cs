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
    INamedTypeSymbol TestClassType,
    ITypeSymbol Type,
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
            
        sourceCodeWriter.Write($"foreach (var {dataName}Accessor in {GetMethodInvocation()})");
        sourceCodeWriter.Write("{");
        sourceCodeWriter.Write($"{enumerableIndexName}++;");
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
            sourceCodeWriter.Write(GenerateVariable(TypesToInject[0].GloballyQualified(), $"{GetMethodInvocation()}{FuncParenthesis()}", ref variableIndex).ToString());
        }
    }

    private void WriteTuples(SourceCodeWriter sourceCodeWriter)
    {
        var tupleVariableName = $"{VariableNamePrefix}Tuples";
        
        if (ArgumentsType == ArgumentsType.Property)
        {
            tupleVariableName += Guid.NewGuid().ToString("N");
        }

        sourceCodeWriter.Write($"var {tupleVariableName} = global::System.TupleExtensions.ToTuple<{string.Join(", ", TypesToInject.Select(x => x.GloballyQualified()))}>({GetMethodInvocation()}{FuncParenthesis()});");
            
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
        if (accessorIndex < 8)
        {
            accessor = $"{tupleVariableName}.Item{accessorIndex}";
        }
        else
        {
            var newIndex = (accessorIndex + 1) % 8;
            var repeatCount = (accessorIndex + 1) / 8;

            if (accessorIndex >= 15)
            {
                newIndex++;
            }
            
            var restAccessor = string.Join(".", Enumerable.Repeat("Rest", repeatCount));
            
            accessor = $"{tupleVariableName}.{restAccessor}.Item{newIndex}";
        }

        var refIndex = index;
        
        sourceCodeWriter.Write(GenerateVariable(tupleType.GloballyQualified(), accessor, ref refIndex).ToString());
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
            
        sourceCodeWriter.Write($"var {dataName} = {dataName}Accessor{FuncParenthesis()};");
            
        if (IsExpandableTuples)
        {
            var tupleVariableName = $"{VariableNamePrefix}Tuples";
            if (ArgumentsType == ArgumentsType.Property)
            {
                tupleVariableName += Guid.NewGuid().ToString("N");
            }
                
            sourceCodeWriter.Write($"var {tupleVariableName} = global::System.TupleExtensions.ToTuple<{string.Join(", ", TypesToInject.Select(x => x.GloballyQualified()))}>({dataName});");

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
            return $"{Type.GloballyQualified()}.{MethodName}({ArgumentsExpression})";
        }
        
        if (Type is INamedTypeSymbol namedTypeSymbol &&
            namedTypeSymbol.Constructors.Any(x => x.Parameters.IsDefaultOrEmpty))
        {
            return $"new {Type.GloballyQualified()}().{MethodName}({ArgumentsExpression})";
        }
        
        if (SymbolEqualityComparer.Default.Equals(Type, TestClassType) && ArgumentsType == ArgumentsType.Method)
        {
            return $"classInstance.{MethodName}({ArgumentsExpression})";
        }
        
        throw new ArgumentException("Only test arguments can reference non-static MethodDataSources");
    }

    public override void CloseScope(SourceCodeWriter sourceCodeWriter)
    {
        if (IsExpandableEnumerable)
        { 
            sourceCodeWriter.Write("}");
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