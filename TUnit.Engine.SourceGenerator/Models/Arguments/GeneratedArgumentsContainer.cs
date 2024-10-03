using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record GeneratedArgumentsContainer : ArgumentsContainer
{
    public GeneratedArgumentsContainer(ArgumentsType ArgumentsType, int AttributeIndex, string TestClassTypeName, string[] GenericArguments, string AttributeDataGeneratorType) : base(ArgumentsType)
    {
        this.AttributeIndex = AttributeIndex;
        this.TestClassTypeName = TestClassTypeName;
        this.GenericArguments = GenericArguments;
        this.AttributeDataGeneratorType = AttributeDataGeneratorType;

        VariableNames = GenerateArgumentVariableNames();
    }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter)
    {
        var objectToGetAttributesFrom = ArgumentsType == ArgumentsType.Method
            ? "methodInfo"
            : $"typeof({TestClassTypeName})";
        
        var guid = Guid.NewGuid().ToString("N");
        sourceCodeWriter.WriteLine($"var {VariableNamePrefix}GeneratedDataArray{guid} = global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<{AttributeDataGeneratorType}>({objectToGetAttributesFrom}).SelectMany(x => x.GenerateDataSources());");
        sourceCodeWriter.WriteLine($"foreach (var {VariableNamePrefix}GeneratedData in {VariableNamePrefix}GeneratedDataArray{guid})");
        sourceCodeWriter.WriteLine("{");

        if (GenericArguments.Length > 1)
        {
            for (var i = 0; i < GenericArguments.Length; i++)
            {
                sourceCodeWriter.WriteLine($"{GenericArguments[i]} {VariableNames[i]} = {VariableNamePrefix}GeneratedData.Item{i + 1};");
            }

            sourceCodeWriter.WriteLine();
        }
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        sourceCodeWriter.WriteLine("}");
    }

    public override string[] VariableNames { get; }

    public override string[] GetArgumentTypes()
    {
        return GenericArguments;
    }

    public int AttributeIndex { get; init; }

    public string TestClassTypeName { get; init; }

    public string[] GenericArguments { get; init; }

    public string AttributeDataGeneratorType { get; init; }

    private string[] GenerateArgumentVariableNames()
    {
        if (GenericArguments.Length == 1)
        {
            return [$"{VariableNamePrefix}GeneratedData"];
        }

        if (ArgumentsType == ArgumentsType.Property)
        {
            throw new Exception("Multiple data values not supported for property injection.");
        }
        
        return Enumerable.Range(0, GenericArguments.Length).Select(i => $"{VariableNamePrefix}{i}").ToArray();
    }
}