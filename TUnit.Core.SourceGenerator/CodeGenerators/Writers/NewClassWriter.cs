﻿using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

internal static class NewClassWriter
{
    public static void ConstructClass(SourceCodeWriter sourceCodeWriter, string typeName, BaseContainer argumentsContainer, ClassPropertiesContainer classPropertiesContainer)
    {
        if (argumentsContainer is ClassConstructorAttributeContainer classConstructorAttributeContainer)
        {
            sourceCodeWriter.WriteLine($"var resettableClassFactoryDelegate = () => new ResettableLazy<{classConstructorAttributeContainer.ClassConstructorType}, {typeName}>();");
            return;
        }
        
        sourceCodeWriter.WriteLine($"var resettableClassFactoryDelegate = () => new ResettableLazy<{typeName}>(() => ");

        sourceCodeWriter.Write($"new {typeName}({argumentsContainer.DataVariables.Select(x => x.Name).ToCommaSeparatedString()})");

        classPropertiesContainer.WriteObjectInitializer(sourceCodeWriter);
        
        sourceCodeWriter.WriteLine(");");
    }
}