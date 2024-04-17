using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class BasicTestArgumentsRetriever
{
    public static IEnumerable<Argument> Parse(IEnumerable<AttributeData> testAndClassAttributes)
    {
        return Array.Empty<Argument>().WithTimeoutArgument(testAndClassAttributes);
    }
}