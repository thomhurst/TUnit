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

        var attributeSyntax = attributeData.ApplicationSyntaxReference.GetSyntax();

        var constructorArgumentSyntaxes = attributeSyntax.DescendantNodes()
            .OfType<AttributeArgumentSyntax>()
            .Where(x => x.NameEquals is null);

        var typedConstantsToExpression =
            constructorArgumentSyntaxes.Zip(attributeData.ConstructorArguments, (syntax, constant) => (syntax, constant));
        
        var constructorArguments = typedConstantsToExpression.Select(x =>
            TypedConstantParser.GetTypedConstantValue(context.SemanticModel, x.syntax.Expression, x.constant.Type));

        var namedArgSyntaxes = attributeSyntax.DescendantNodes()
            .OfType<AttributeArgumentSyntax>()
            .Where(x => x.NameEquals is not null)
            .ToArray();

        var namedArguments = attributeData.NamedArguments.Select(x =>
            $"{x.Key} = {TypedConstantParser.GetTypedConstantValue(context.SemanticModel, namedArgSyntaxes.First(stx => stx.NameEquals?.Name.Identifier.ValueText == x.Key).Expression, x.Value.Type)},");

        return $$"""
                 new {{attributeData.AttributeClass!.GloballyQualified()}}({{string.Join(", ", constructorArguments)}})
                 {
                     {{string.Join(" ", namedArguments)}}
                 }
                 """;
    }
}