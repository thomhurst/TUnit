using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Writers;

internal static class NewClassWriter
{
    public static string ConstructClass(string typeName, ArgumentsContainer argumentsContainer)
    {
        if (argumentsContainer is ClassConstructorAttributeContainer)
        {
            return $"classConstructor.Create<{typeName}>()";
        }
        
        return $"new {typeName}({argumentsContainer.GenerateArgumentVariableNames().ToCommaSeparatedString()})";
    }
}