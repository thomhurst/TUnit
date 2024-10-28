using TUnit.Core.SourceGenerator.Enums;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record MethodDataSourceAttributeContainer(
    ArgumentsType ArgumentsType,
    string TestClassTypeName,
    string TypeName,
    string MethodName,
    bool IsStatic,
    bool IsEnumerableData,
    string[] TupleTypes,
    string MethodReturnType,
    string ArgumentsExpression)
    : ArgumentsContainer(ArgumentsType)
{
    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
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

                    var refIndex = index;
                    
                    sourceCodeWriter.WriteLine(GenerateVariable(tupleType, $"{tupleVariableName}.Item{index+1}", ref refIndex).ToString());
                }
            }
            else
            {
                AddVariable(new Variable
                {
                    Type = "var", 
                    Name = dataName, 
                    Value = GetMethodInvocation()   
                });
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

                var refIndex = index;
                
                sourceCodeWriter.WriteLine(GenerateVariable(tupleType, $"{tupleVariableName}.Item{index+1}", ref refIndex).ToString());
            }
        }
        else
        {
            sourceCodeWriter.WriteLine(GenerateVariable(MethodReturnType, GetMethodInvocation(), ref variableIndex).ToString());
        }
        
        sourceCodeWriter.WriteLine();
    }

    private string GetMethodInvocation()
    {
        if (IsStatic)
        {
            return $"{TypeName}.{MethodName}({ArgumentsExpression})";
        }
        
        return $"resettableClassFactory.Value.{MethodName}({ArgumentsExpression})";
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        if (IsEnumerableData)
        { 
            sourceCodeWriter.WriteLine("}");
        }
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