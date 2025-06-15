using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public class AttributeWriter
{
    public static void WriteAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        ImmutableArray<AttributeData> attributeDatas)
    {
        var dataAttributeInterface =
            context.SemanticModel.Compilation.GetTypeByMetadataName(WellKnownFullyQualifiedClassNames.IAsyncDataSourceGeneratorAttribute
                .WithoutGlobalPrefix);

        attributeDatas = attributeDatas.RemoveAll(x => x.AttributeClass?.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, dataAttributeInterface)) == true);

        if (attributeDatas.Length == 0)
        {
            sourceCodeWriter.Write("[],");
            return;
        }

        sourceCodeWriter.Write("[");
        for (var index = 0; index < attributeDatas.Length; index++)
        {
            var attributeData = attributeDatas[index];

            if (attributeData.ApplicationSyntaxReference is null)
            {
                continue;
            }

            WriteAttribute(sourceCodeWriter, context, attributeData);

            if (index != attributeDatas.Length - 1)
            {
                sourceCodeWriter.Write(",");
            }

            sourceCodeWriter.WriteLine();
        }
        sourceCodeWriter.Write("],");
    }

    public static void WriteAttribute(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        AttributeData attributeData)
    {
        sourceCodeWriter.Write(GetAttributeObjectInitializer(context, attributeData, sourceCodeWriter.TabLevel));
    }

    public static string GetAttributeObjectInitializer(GeneratorAttributeSyntaxContext context,
        AttributeData attributeData, int indentLevel)
    {
        var sourceCodeWriter = new SourceCodeWriter(indentLevel);

        var syntax = attributeData.ApplicationSyntaxReference?.GetSyntax();

        if (syntax is null)
        {
            WriteAttributeWithoutSyntax(sourceCodeWriter, context, attributeData);
            return sourceCodeWriter.ToString();
        }

        var arguments = syntax.ChildNodes()
            .OfType<AttributeArgumentListSyntax>()
            .FirstOrDefault()
            ?.Arguments ?? [];

        var properties = arguments.Where(x => x.NameEquals != null);

        var constructorArgs = arguments.Where(x => x.NameEquals == null);

        var attributeName = attributeData.AttributeClass!.GloballyQualified();

        var formattedConstructorArgs = string.Join(", ", constructorArgs.Select(x => FormatConstructorArgument(context, x)));

        var formattedProperties = properties.Select(x => FormatProperty(context, x)).ToArray();

        sourceCodeWriter.Write($"new {attributeName}({formattedConstructorArgs})");

        if (formattedProperties.Length == 0
            && !HasNestedDataGeneratorProperties(attributeData))
        {
            return sourceCodeWriter.ToString();
        }

        sourceCodeWriter.WriteLine();
        sourceCodeWriter.Write("{");
        foreach (var property in formattedProperties)
        {
            sourceCodeWriter.Write($"{property},");
        }

        WriteDataSourceGeneratorProperties(sourceCodeWriter, context, attributeData);

        sourceCodeWriter.Write("}");

        return sourceCodeWriter.ToString();
    }

    private static bool HasNestedDataGeneratorProperties(AttributeData attributeData)
    {
        if (attributeData.AttributeClass is not { } attributeClass)
        {
            return false;
        }

        if (attributeClass.GetMembersIncludingBase().OfType<IPropertySymbol>().Any(x => x.GetAttributes().Any(a => a.IsDataSourceAttribute())))
        {
            return true;
        }

        return false;
    }

    private static void WriteDataSourceGeneratorProperties(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, AttributeData attributeData)
    {
        foreach (var propertySymbol in attributeData.AttributeClass?.GetMembers().OfType<IPropertySymbol>() ?? [])
        {
            if (propertySymbol.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (propertySymbol.GetAttributes().FirstOrDefault(x => x.IsDataSourceAttribute()) is not { } dataSourceAttribute)
            {
                continue;
            }

            sourceCodeWriter.Write($"{propertySymbol.Name} = ");

            var innerAttribute = GetAttributeObjectInitializer(context, dataSourceAttribute, sourceCodeWriter.TabLevel);

            sourceCodeWriter.Write(DataSourceGeneratorContainer.GetPropertyAssignmentFromDataSourceGeneratorAttribute(innerAttribute, context, attributeData.AttributeClass!, propertySymbol, sourceCodeWriter.TabLevel, true));
        }
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

    public static void WriteAttributeWithoutSyntax(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
    AttributeData attributeData)
    {
        var attributeName = attributeData.AttributeClass!.GloballyQualified();

        var constructorArgs = attributeData.ConstructorArguments.Select(TypedConstantParser.GetRawTypedConstantValue);
        var formattedConstructorArgs = string.Join(", ", constructorArgs);

        var namedArgs = attributeData.NamedArguments.Select(arg => $"{arg.Key} = {TypedConstantParser.GetRawTypedConstantValue(arg.Value)}");
        var formattedNamedArgs = string.Join(", ", namedArgs);

        sourceCodeWriter.Write($"new {attributeName}({formattedConstructorArgs})");

        if (string.IsNullOrEmpty(formattedNamedArgs))
        {
            return;
        }

        sourceCodeWriter.WriteLine();
        sourceCodeWriter.Write("{");
        sourceCodeWriter.Write($"{formattedNamedArgs}");
        sourceCodeWriter.Write("}");
    }
}
