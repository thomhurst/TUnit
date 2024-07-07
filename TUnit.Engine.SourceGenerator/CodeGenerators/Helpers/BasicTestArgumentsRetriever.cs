using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class BasicTestArgumentsRetriever
{
    public static ArgumentsContainer Parse()
    {
        return new ArgumentsContainer
        {
            DataAttribute = null,
            DataAttributeIndex = null,
            IsEnumerableData = false,
            Arguments = []
        };
    }
}