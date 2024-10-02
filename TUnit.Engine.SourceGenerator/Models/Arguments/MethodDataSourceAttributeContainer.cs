using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record MethodDataSourceAttributeContainer : DataAttributeContainer
{
    public required string TestClassTypeName { get; init; }
    public required string TypeName { get; init; }
    public required string MethodName { get; init; }
    public required bool IsStatic { get; init; }
    public required bool IsEnumerableData { get; init; }
    public required string[] TupleTypes { get; init; }
    public required string MethodReturnType { get; set; }

    public override void GenerateInvocationStatements(SourceCodeWriter sourceCodeWriter)
    {
        var argsVariableNamePrefix = ArgumentsType == ArgumentsType.ClassConstructor
            ? VariableNames.ClassArg
            : VariableNames.MethodArg;
        
        if (IsEnumerableData)
        {
            var enumerableIndexName = ArgumentsType == ArgumentsType.ClassConstructor
                ? VariableNames.EnumerableClassDataIndex
                : VariableNames.EnumerableTestDataIndex;
            
            var dataName = ArgumentsType == ArgumentsType.ClassConstructor
                ? VariableNames.ClassData
                : VariableNames.MethodData;
            
            sourceCodeWriter.WriteLine($"foreach (var {dataName} in {GetMethodInvocation()})");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"{enumerableIndexName}++;");
            
            if (TupleTypes.Any())
            {
                sourceCodeWriter.WriteLine($"var {argsVariableNamePrefix}Tuples = global::System.TupleExtensions.ToTuple<{string.Join(", ", TupleTypes)}>({dataName});");

                for (var index = 0; index < TupleTypes.Length; index++)
                {
                    var tupleType = TupleTypes[index];
                
                    sourceCodeWriter.WriteLine($"{tupleType} {argsVariableNamePrefix}{index} = {argsVariableNamePrefix}Tuples.Item{index+1};");
                }
            }
            
        }
        else if (TupleTypes.Any())
        {
            sourceCodeWriter.WriteLine($"var {argsVariableNamePrefix}Tuples = global::System.TupleExtensions.ToTuple<{string.Join(", ", TupleTypes)}>({GetMethodInvocation()});");
            
            for (var index = 0; index < TupleTypes.Length; index++)
            {
                var tupleType = TupleTypes[index];
                
                sourceCodeWriter.WriteLine($"{tupleType} {argsVariableNamePrefix}{index} = {argsVariableNamePrefix}Tuples.Item{index+1};");
            }
        }
        else
        {
            sourceCodeWriter.WriteLine($"{MethodReturnType} {argsVariableNamePrefix} = {GetMethodInvocation()};");
        }
        
        sourceCodeWriter.WriteLine();
    }

    private string GetMethodInvocation()
    {
        if (IsStatic)
        {
            return $"{TypeName}.{MethodName}()";
        }
        
        return $"resettableClassFactory.Value.{MethodName}()";
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        if (IsEnumerableData)
        {
            if (ArgumentsType == ArgumentsType.Method)
            {
                sourceCodeWriter.WriteLine("resettableClassFactory = resettableClassFactoryDelegate();");
            }
            
            sourceCodeWriter.WriteLine("}");
        }
    }

    public override string[] GenerateArgumentVariableNames()
    {
        var argsVariablePrefix = ArgumentsType == ArgumentsType.ClassConstructor
            ? VariableNames.ClassArg
            : VariableNames.MethodArg;
        
        if (TupleTypes.Any())
        {
            return TupleTypes
                .Select((x, i) => $"{argsVariablePrefix}{i}")
                .ToArray();
        }
        
        if (IsEnumerableData)
        {
            return
            [
                ArgumentsType == ArgumentsType.ClassConstructor
                    ? VariableNames.ClassData
                    : VariableNames.MethodData
            ];
        }
        
        return
        [
            argsVariablePrefix
        ];
    }

    public override string[] GetArgumentTypes()
    {
        if (TupleTypes.Any())
        {
            return TupleTypes;
        }
        
        return [MethodReturnType];
    }
}