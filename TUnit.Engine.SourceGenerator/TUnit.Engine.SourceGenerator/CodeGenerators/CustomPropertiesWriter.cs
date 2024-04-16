using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

public class CustomPropertiesWriter
{
    public static void WriteCustomProperties(AttributeData[] methodAndClassAttributes,
        SourceCodeWriter sourceBuilder)
    {
        var propertyAttributes = methodAndClassAttributes.Where(x =>
            x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            == WellKnownFullyQualifiedClassNames.CustomPropertyAttribute).ToList();

        if (!propertyAttributes.Any())
        {
            sourceBuilder.WriteLine("CustomProperties = new global::System.Collections.Generic.Dictionary<string, string>(),");
            return;
        }
        
        sourceBuilder.WriteLine("CustomProperties = new global::System.Collections.Generic.Dictionary<string, string>()");
        sourceBuilder.WriteLine("{");
        
        foreach (var propertyAttribute in propertyAttributes)
        {
            var name = TypedConstantParser.GetTypedConstantValue(propertyAttribute.ConstructorArguments[0]);
            var value = TypedConstantParser.GetTypedConstantValue(propertyAttribute.ConstructorArguments[1]);
            
            sourceBuilder.WriteLine($"[\"{name}\"] = \"{value}\",");
        }
        
        sourceBuilder.WriteLine("},");
    }
}