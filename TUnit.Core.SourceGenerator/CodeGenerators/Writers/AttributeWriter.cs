using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public class AttributeWriter
{
    public static string[] WriteAttributes(GeneratorAttributeSyntaxContext context, ImmutableArray<AttributeData> attributeDatas)
    {
        return attributeDatas
            .Where(x => x.AttributeClass?.ContainingAssembly?.Name != "System.Runtime")
            .Select(x => WriteAttribute(context, x))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
    }

    public static string WriteAttribute(GeneratorAttributeSyntaxContext context, AttributeData attributeData)
    {
        if (attributeData.ApplicationSyntaxReference is null
            || attributeData.AttributeClass?.ContainingAssembly?.Name == "System.Runtime")
        {
            return string.Empty;
        }

        var syntax = attributeData.ApplicationSyntaxReference.GetSyntax();

        var arguments = syntax.ChildNodes()
            .OfType<AttributeArgumentListSyntax>()
            .FirstOrDefault()
            ?.Arguments ?? [];

        var properties = arguments.Where(x => x.NameEquals != null);
        
        var constructorArgs = arguments.Where(x => x.NameEquals == null);

        var attributeName = attributeData.AttributeClass!.GloballyQualified();

        var formattedConstructorArgs = string.Join(", ", constructorArgs.Select(x => FormatConstructorArgument(context, x)));

        var formattedProperties = string.Join(",\r\n", properties.Select(x => FormatProperty(context, x)));
        
        return $$"""
                 new {{attributeName}}({{formattedConstructorArgs}})
                 {
                     {{formattedProperties}}
                 }
                 """;
    }

    private static string FormatConstructorArgument(GeneratorAttributeSyntaxContext context, AttributeArgumentSyntax attributeArgumentSyntax)
    {
        if (attributeArgumentSyntax.NameColon is not null)
        {
            return $"{attributeArgumentSyntax.NameColon!.Name}: {attributeArgumentSyntax.Expression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(context.SemanticModel))!.ToFullString()}";
        }
        
        return attributeArgumentSyntax.Accept(new FullyQualifiedWithGlobalPrefixRewriter(context.SemanticModel))!.ToFullString();
    }

    private static string FormatProperty(GeneratorAttributeSyntaxContext context, AttributeArgumentSyntax attributeArgumentSyntax)
    {
        return $"{attributeArgumentSyntax.NameEquals!.Name} = {attributeArgumentSyntax.Expression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(context.SemanticModel))!.ToFullString()}";
    }
}