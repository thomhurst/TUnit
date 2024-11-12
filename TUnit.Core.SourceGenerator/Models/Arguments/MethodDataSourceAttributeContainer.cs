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
    public override void OpenScope(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        if (!IsEnumerableData)
        {
            return;
        }
        
        var dataName = ArgumentsType == ArgumentsType.ClassConstructor
            ? CodeGenerators.VariableNames.ClassData
            : CodeGenerators.VariableNames.MethodData;
            
        sourceCodeWriter.WriteLine($"for (var {dataName}CurrentIndex = 0; {dataName}CurrentIndex < {GetMethodInvocation()}.Count(); {dataName}CurrentIndex++)");
        sourceCodeWriter.WriteLine("{");
    }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        var dataName = ArgumentsType == ArgumentsType.ClassConstructor
            ? CodeGenerators.VariableNames.ClassData
            : CodeGenerators.VariableNames.MethodData;
        
        if (IsEnumerableData)
        {
            if (ArgumentsType == ArgumentsType.Property)
            {
                throw new Exception("Property Injection is not supported with Enumerable data");
            }
            
            if (TupleTypes.Any())
            {
                var tupleVariableName = $"{VariableNamePrefix}Tuples";
                if (ArgumentsType == ArgumentsType.Property)
                {
                    tupleVariableName += Guid.NewGuid().ToString("N");
                }
                
                sourceCodeWriter.WriteLine($"var {tupleVariableName} = global::System.TupleExtensions.ToTuple<{string.Join(", ", TupleTypes)}>({GetMethodInvocation()}.ElementAt({dataName}CurrentIndex));");

                for (var index = 0; index < TupleTypes.Length; index++)
                {
                    var tupleType = TupleTypes[index];

                    var refIndex = index;
                    
                    sourceCodeWriter.WriteLine(GenerateVariable(tupleType, $"{tupleVariableName}.Item{index+1}", ref refIndex).ToString());
                }
            }
            else
            {
                sourceCodeWriter.WriteLine($"var {dataName} = {GetMethodInvocation()}.ElementAt({dataName}CurrentIndex);");

                AddVariable(new Variable
                {
                    Type = "var", 
                    Name = dataName, 
                    Value = GetMethodInvocation() + $".ElementAt({dataName}CurrentIndex)"   
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
        
        return $"new {TestClassTypeName}().{MethodName}({ArgumentsExpression})";
    }

    public override void CloseScope(SourceCodeWriter sourceCodeWriter)
    {
        if (IsEnumerableData)
        { 
            var enumerableIndexName = ArgumentsType == ArgumentsType.ClassConstructor
                ? CodeGenerators.VariableNames.ClassDataIndex
                : CodeGenerators.VariableNames.TestMethodDataIndex;
            
            sourceCodeWriter.WriteLine($"{enumerableIndexName}++;");

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