using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class NewClassWriter
{
    public static void ConstructClass(ICodeWriter sourceCodeWriter, string typeName, BaseContainer argumentsContainer, ClassPropertiesContainer classPropertiesContainer)
    {
        if (argumentsContainer is ClassConstructorAttributeContainer classConstructorAttributeContainer)
        {
            sourceCodeWriter.Append($"var resettableClassFactoryDelegate = () => new ResettableLazy<{classConstructorAttributeContainer.ClassConstructorType}, {typeName}>(sessionId, testBuilderContext);");
            return;
        }

        sourceCodeWriter.Append($"var resettableClassFactoryDelegate = () => new ResettableLazy<{typeName}>(() => ");

        sourceCodeWriter.Append($"new {typeName}({argumentsContainer.DataVariables.Select(x => x.Name).ToCommaSeparatedString()})");

        classPropertiesContainer.WriteObjectInitializer(sourceCodeWriter);

        sourceCodeWriter.Append(", sessionId, testBuilderContext);");
    }
}
