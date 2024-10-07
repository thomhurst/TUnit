using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record MethodDataSourceAttributeContainer : ArgumentsContainer
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
    }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        if(!VariableNames.Any())
        {
            GenerateArgumentVariableNames(ref variableIndex);
        }

        if (IsEnumerableData)
        {
            if (ArgumentsType == ArgumentsType.Property)
            {
                throw new Exception("Property Injection is not supported with Enumerable data");
            }
            
            var enumerableIndexName = ArgumentsType == ArgumentsType.ClassConstructor
                ? CodeGenerators.VariableNames.ClassDataIndex
                : CodeGenerators.VariableNames.TestMethodDataIndex;
            
            var dataName = ArgumentsType == ArgumentsType.ClassConstructor
                ? CodeGenerators.VariableNames.ClassData
                : CodeGenerators.VariableNames.MethodData;
            
            sourceCodeWriter.WriteLine($"foreach (var {dataName} in {GetMethodInvocation()})");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"{enumerableIndexName}++;");
            
            if (TupleTypes.Any())
            {
                var tupleVariableName = $"{VariableNamePrefix}Tuples";
                if (ArgumentsType == ArgumentsType.Property)
                {
                    tupleVariableName += Guid.NewGuid().ToString("N");
                }
                
                sourceCodeWriter.WriteLine($"var {tupleVariableName} = global::System.TupleExtensions.ToTuple<{string.Join(", ", TupleTypes)}>({dataName});");

                for (var index = 0; index < TupleTypes.Length; index++)
                {
                    var tupleType = TupleTypes[index];
                
                    sourceCodeWriter.WriteLine($"{tupleType} {VariableNames.ElementAt(index)} = {tupleVariableName}.Item{index+1};");
                }
            }
            
        }
        else if (TupleTypes.Any())
        {
            var tupleVariableName = $"{VariableNamePrefix}Tuples";
            if (ArgumentsType == ArgumentsType.Property)
            {
                tupleVariableName += Guid.NewGuid().ToString("N");
            }

            sourceCodeWriter.WriteLine($"var {tupleVariableName} = global::System.TupleExtensions.ToTuple<{string.Join(", ", TupleTypes)}>({GetMethodInvocation()});");
            
            for (var index = 0; index < TupleTypes.Length; index++)
            {
                var tupleType = TupleTypes[index];
                
                sourceCodeWriter.WriteLine($"{tupleType} {VariableNames.ElementAt(index)} = {tupleVariableName}.Item{index+1};");
            }
        }
        else
        {
            sourceCodeWriter.WriteLine($"{MethodReturnType} {VariableNames.ElementAt(0)} = {GetMethodInvocation()};");
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
    
    public void GenerateArgumentVariableNames(ref int variableIndex)
    {
        if (TupleTypes.Any())
        {
            for (var index = 0; index < TupleTypes.Length; index++)
            {
                GenerateVariableName(ref variableIndex);
            }

            return;
        }
        
        if (IsEnumerableData)
        {
            AddVariable(ArgumentsType == ArgumentsType.ClassConstructor
                ? CodeGenerators.VariableNames.ClassData
                : CodeGenerators.VariableNames.MethodData);
            return;
        }
        
        GenerateVariableName(ref variableIndex);
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
    public string TypeName { get; }
    public string MethodName { get; }
    public bool IsStatic { get; }
    public bool IsEnumerableData { get; }
    public string[] TupleTypes { get; }
    public string MethodReturnType { get; }
}