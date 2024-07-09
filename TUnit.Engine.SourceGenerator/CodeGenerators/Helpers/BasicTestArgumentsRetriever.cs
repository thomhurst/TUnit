using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class BasicTestArgumentsRetriever
{
    private static readonly ArgumentsContainer EmptyArgumentsContainer = new()
    {
        DataAttribute = null,
        DataAttributeIndex = null,
        IsEnumerableData = false,
        Arguments = []
    };

    public static ArgumentsContainer Parse()
    {
        return EmptyArgumentsContainer;
    }
}