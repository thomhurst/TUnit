using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal class TimeoutCancellationTokenRetriever
{
    public static Argument? GetCancellationTokenArgument(IEnumerable<AttributeData> attributes)
    {
        var timeoutAttribute = attributes.SafeFirstOrDefault(x =>
            x.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames.TimeoutAttribute.WithGlobalPrefix) == true);
        
        if (timeoutAttribute != null)
        {
            return new Argument(ArgumentSource.TimeoutAttribute, "global::System.Threading.CancellationToken", "cancellationToken)");
        }

        return null;
    }
}