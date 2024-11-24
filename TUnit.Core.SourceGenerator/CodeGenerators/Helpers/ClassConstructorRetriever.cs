using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Arguments;

namespace TUnit.Core.SourceGenerator.Helpers;

public class ClassConstructorRetriever
{
    public static ArgumentsContainer Parse(AttributeData dataAttribute, int index)
    {
        var type = dataAttribute.AttributeClass!.TypeArguments.First();

        // TODO: Property injection?
        return new ClassConstructorAttributeContainer(ArgumentsType.ClassConstructor)
        {
            AttributeIndex = index,
            Attribute = dataAttribute,
            ClassConstructorType = type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            ArgumentsType = ArgumentsType.ClassConstructor,
            DisposeAfterTest = dataAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ?? true,
        };
    }
}