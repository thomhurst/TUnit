using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class NewClassWriter
{
    public static void ConstructClass(SourceCodeWriter sourceCodeWriter, string typeName, BaseContainer argumentsContainer, ClassPropertiesContainer classPropertiesContainer)
    {
        if (argumentsContainer is ClassConstructorAttributeContainer classConstructorAttributeContainer)
        {
            sourceCodeWriter.WriteLine($"var resettableClassFactoryDelegate = () => new ResettableLazy<{classConstructorAttributeContainer.ClassConstructorType}, {typeName}>(sessionId);");
            return;
        }
        
        sourceCodeWriter.WriteLine($"var resettableClassFactoryDelegate = () => new ResettableLazy<{typeName}>(() => ");

        sourceCodeWriter.WriteLine($"new {typeName}({argumentsContainer.DataVariables.Select(x => x.Name).ToCommaSeparatedString()})");

        classPropertiesContainer.WriteObjectInitializer(sourceCodeWriter);
        
        sourceCodeWriter.WriteLine(", sessionId);");
    }
}