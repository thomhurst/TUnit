using TUnit.Core.SourceGenerator.Arguments;

namespace TUnit.Core.SourceGenerator.Writers;

public static class NewClassWriter
{
    public static void ConstructClass(SourceCodeWriter sourceCodeWriter, string typeName, BaseContainer argumentsContainer, ClassPropertiesContainer classPropertiesContainer)
    {
        if (argumentsContainer is ClassConstructorAttributeContainer classConstructorAttributeContainer)
        {
            sourceCodeWriter.WriteLine($"var resettableClassFactoryDelegate = () => new ResettableLazy<{classConstructorAttributeContainer.ClassConstructorType}, {typeName}>(sessionId, testBuilderContext);");
            return;
        }
        
        sourceCodeWriter.WriteLine($"var resettableClassFactoryDelegate = () => new ResettableLazy<{typeName}>(() => ");

        sourceCodeWriter.WriteLine($"new {typeName}({argumentsContainer.DataVariables.Select(x => x.Name).ToCommaSeparatedString()})");

        classPropertiesContainer.WriteObjectInitializer(sourceCodeWriter);
        
        sourceCodeWriter.WriteLine(", sessionId, testBuilderContext);");
    }
}