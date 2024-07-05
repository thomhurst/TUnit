using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.Extensions;

internal static class ArgumentExtensions
{
    public static IEnumerable<Argument> WithTimeoutArgument(this IEnumerable<Argument> arguments,
        IEnumerable<AttributeData> testAndClassAttributes)
    {
        var timeoutArgument = TimeoutCancellationTokenRetriever.GetCancellationTokenArgument(testAndClassAttributes);

        return timeoutArgument == null ? arguments : arguments.Append(timeoutArgument);
    }
}