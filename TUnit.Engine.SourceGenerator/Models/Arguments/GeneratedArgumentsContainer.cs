using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record GeneratedArgumentsContainer : ArgumentsContainer
{
    public required int AttributeIndex { get; set; }
    public required string TestClassTypeName { get; init; }
    public required string[] GenericArguments { get; init; }
    public required string AttributeDataGeneratorType { get; init; }

    public override void GenerateInvocationStatements(SourceCodeWriter sourceCodeWriter)
    {
        var objectToGetAttributesFrom = ArgumentsType == ArgumentsType.Method
            ? "methodInfo"
            : $"typeof({TestClassTypeName})";
        
        sourceCodeWriter.WriteLine($"var {VariableNamePrefix}GeneratedDataArray = global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<{AttributeDataGeneratorType}>({objectToGetAttributesFrom}).SelectMany(x => x.GenerateDataSources());");
        sourceCodeWriter.WriteLine($"foreach (var {VariableNamePrefix}GeneratedData in {VariableNamePrefix}GeneratedDataArray)");
        sourceCodeWriter.WriteLine("{");

        if (GenericArguments.Length > 1)
        {
            for (var i = 0; i < GenericArguments.Length; i++)
            {
                sourceCodeWriter.WriteLine($"{GenericArguments[i]} {VariableNamePrefix}{i} = {VariableNamePrefix}GeneratedData.Item{i + 1};");
            }

            sourceCodeWriter.WriteLine();
        }
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        sourceCodeWriter.WriteLine("}");
    }

    public override string[] GenerateArgumentVariableNames()
    {
        if (GenericArguments.Length == 1)
        {
            return [$"{VariableNamePrefix}GeneratedData"];
        }
        
        return Enumerable.Range(0, GenericArguments.Length).Select(i => $"{VariableNamePrefix}{i}").ToArray();
    }

    public override string[] GetArgumentTypes()
    {
        return GenericArguments;
    }
}