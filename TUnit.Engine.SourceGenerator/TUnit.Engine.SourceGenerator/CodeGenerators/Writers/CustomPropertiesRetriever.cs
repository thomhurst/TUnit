using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Writers;

public class CustomPropertiesRetriever
{
    public static IEnumerable<string> GetCustomProperties(IEnumerable<AttributeData> methodAndClassAttributes)
    {
        var propertyAttributes = methodAndClassAttributes
            .Where(x => x.GetFullyQualifiedAttributeTypeName()
                        == WellKnownFullyQualifiedClassNames.CustomPropertyAttribute.WithGlobalPrefix)
            .ToList();

        if (!propertyAttributes.Any())
        {
            yield return "CustomProperties = new global::System.Collections.Generic.Dictionary<string, string>(),";
            yield break;
        }
        
        yield return "CustomProperties = new global::System.Collections.Generic.Dictionary<string, string>()";
        yield return "{";
        
        foreach (var propertyAttribute in propertyAttributes)
        {
            var name = TypedConstantParser.GetTypedConstantValue(propertyAttribute.ConstructorArguments[0]);
            var value = TypedConstantParser.GetTypedConstantValue(propertyAttribute.ConstructorArguments[1]);
            
            yield return $"[\"{name}\"] = \"{value}\",";
        }
        
        yield return "},";
    }
}