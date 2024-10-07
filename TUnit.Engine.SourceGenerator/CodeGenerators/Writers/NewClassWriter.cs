using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Writers;

internal static class NewClassWriter
{
    public static void ConstructClass(SourceCodeWriter sourceCodeWriter, string typeName, BaseContainer argumentsContainer, ClassPropertiesContainer classPropertiesContainer)
    {
        if (argumentsContainer is ClassConstructorAttributeContainer)
        {
            sourceCodeWriter.Write($"classConstructor.Create<{typeName}>()");
            return;
        }
        
        sourceCodeWriter.Write($"new {typeName}({argumentsContainer.VariableNames.ToCommaSeparatedString()})");

        classPropertiesContainer.WriteObjectInitializer(sourceCodeWriter);
    }
}