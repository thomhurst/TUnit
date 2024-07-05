using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Writers;

public class CustomPropertiesRetriever
{
    public static IEnumerable<string> GetCustomProperties(IEnumerable<AttributeData> methodAndClassAttributes)
    {
        var propertyAttributes = methodAndClassAttributes
            .Where(x => x.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames.CustomPropertyAttribute.WithGlobalPrefix) == true)
            .ToList();

        if (!propertyAttributes.Any())
        {
            yield return "CustomProperties = new global::System.Collections.Generic.Dictionary<string, string>(),";
            yield break;
        }
        
        yield return $"CustomProperties = methodInfo.GetCustomAttributes<{WellKnownFullyQualifiedClassNames.CustomPropertyAttribute.WithGlobalPrefix}>().ToDictionary(x => x.Name, x => x.Value),";
    }
}