using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record MethodDataSourceAttributeContainer : DataAttributeContainer
{
    public MethodDataSourceAttributeContainer(ArgumentsType ArgumentsType, string TestClassTypeName, string TypeName, string MethodName, bool IsStatic, bool IsEnumerableData, string[] TupleTypes, string MethodReturnType) : base(ArgumentsType)
    {
        this.TestClassTypeName = TestClassTypeName;
        this.TypeName = TypeName;
        this.MethodName = MethodName;
        this.IsStatic = IsStatic;
        this.IsEnumerableData = IsEnumerableData;
        this.TupleTypes = TupleTypes;
        this.MethodReturnType = MethodReturnType;

        VariableNames = GenerateArgumentVariableNames();
    }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter)
    {
        if (IsEnumerableData)
        {
            if (ArgumentsType == ArgumentsType.Property)
            {
                throw new Exception("Property Injection is not supported with Enumerable data");
            }
            
            var enumerableIndexName = ArgumentsType == ArgumentsType.ClassConstructor
                ? CodeGenerators.VariableNames.EnumerableClassDataIndex
                : CodeGenerators.VariableNames.EnumerableTestDataIndex;
            
            var dataName = ArgumentsType == ArgumentsType.ClassConstructor
                ? CodeGenerators.VariableNames.ClassData
                : CodeGenerators.VariableNames.MethodData;
            
            sourceCodeWriter.WriteLine($"foreach (var {dataName} in {GetMethodInvocation()})");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"{enumerableIndexName}++;");
            
            if (TupleTypes.Any())
            {
                var tupleVariableName = $"tuples{Guid.NewGuid():N}";
                sourceCodeWriter.WriteLine($"var {tupleVariableName} = global::System.TupleExtensions.ToTuple<{string.Join(", ", TupleTypes)}>({dataName});");

                for (var index = 0; index < TupleTypes.Length; index++)
                {
                    var tupleType = TupleTypes[index];
                
                    sourceCodeWriter.WriteLine($"{tupleType} {VariableNames[index]} = {tupleVariableName}.Item{index+1};");
                }
            }
            
        }
        else if (TupleTypes.Any())
        {
            var tupleVariableName = $"tuples{Guid.NewGuid():N}";

            sourceCodeWriter.WriteLine($"var {tupleVariableName} = global::System.TupleExtensions.ToTuple<{string.Join(", ", TupleTypes)}>({GetMethodInvocation()});");
            
            for (var index = 0; index < TupleTypes.Length; index++)
            {
                var tupleType = TupleTypes[index];
                
                sourceCodeWriter.WriteLine($"{tupleType} {VariableNames[index]} = {tupleVariableName}.Item{index+1};");
            }
        }
        else
        {
            sourceCodeWriter.WriteLine($"{MethodReturnType} {VariableNames[0]} = {GetMethodInvocation()};");
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

    public override string[] VariableNames { get; }

    public string[] GenerateArgumentVariableNames()
    {
        if (TupleTypes.Any())
        {
            return TupleTypes
                .Select((_, i) => GenerateUniqueVariableName())
                .ToArray();
        }
        
        if (IsEnumerableData)
        {
            return
            [
                ArgumentsType == ArgumentsType.ClassConstructor
                    ? CodeGenerators.VariableNames.ClassData
                    : CodeGenerators.VariableNames.MethodData
            ];
        }
        
        return
        [
            GenerateUniqueVariableName()
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

    public string TestClassTypeName { get; init; }
    public string TypeName { get; init; }
    public string MethodName { get; init; }
    public bool IsStatic { get; init; }
    public bool IsEnumerableData { get; init; }
    public string[] TupleTypes { get; init; }
    public string MethodReturnType { get; init; }
}