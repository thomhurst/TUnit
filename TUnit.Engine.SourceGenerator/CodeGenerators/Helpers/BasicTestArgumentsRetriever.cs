using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class BasicTestArgumentsRetriever
{
    public static ArgumentsContainer Parse(IEnumerable<AttributeData> testAndClassAttributes)
    {
        return new ArgumentsContainer
        {
            DataAttribute = null,
            DataAttributeIndex = null,
            IsEnumerableData = false,
            Arguments = Array.Empty<Argument>()
        };
    }
}