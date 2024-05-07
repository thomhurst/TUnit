using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal class TimeoutCancellationTokenRetriever
{
    public static Argument? GetCancellationTokenArgument(IEnumerable<AttributeData> attributes)
    {
        var timeoutAttribute = attributes.FirstOrDefault(x =>
            x.GetFullyQualifiedAttributeTypeName()
            == WellKnownFullyQualifiedClassNames.TimeoutAttribute.WithGlobalPrefix);
        
        if (timeoutAttribute != null)
        {
            var timeoutInMillis = (int) timeoutAttribute.ConstructorArguments.First().Value!;
            return new Argument(ArgumentSource.TimeoutAttribute, "global::System.Threading.CancellationToken", $"global::TUnit.Engine.EngineCancellationToken.CreateToken(global::System.TimeSpan.FromMilliseconds({timeoutInMillis}))");
        }

        return null;
    }
}