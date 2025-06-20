using System.Collections.Immutable;
using System.Linq;
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

    public static void WriteAttributeMetadatas(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        ImmutableArray<AttributeData> attributeDatas, string targetElement, string? targetMemberName = null, string? targetTypeName = null, bool includeClassMetadata = false)
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

            WriteAttributeMetadata(sourceCodeWriter, context, attributeData, targetElement, targetMemberName, targetTypeName, includeClassMetadata);

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

    public static void WriteAttributeMetadata(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        AttributeData attributeData, string targetElement, string? targetMemberName, string? targetTypeName, bool includeClassMetadata = false)
    {
        sourceCodeWriter.Write("new global::TUnit.Core.AttributeMetadata");
        sourceCodeWriter.Write("{");
        sourceCodeWriter.Write($"Instance = {GetAttributeObjectInitializer(context, attributeData, sourceCodeWriter.TabLevel)},");
        sourceCodeWriter.Write($"TargetElement = global::TUnit.Core.TestAttributeTarget.{targetElement},");

        if (targetMemberName != null)
        {
            sourceCodeWriter.Write($"TargetMemberName = \"{targetMemberName}\",");
        }

        if (targetTypeName != null)
        {
            sourceCodeWriter.Write($"TargetType = typeof({targetTypeName}),");
        }
        
        // Add ClassMetadata if requested and not a system attribute
        if (includeClassMetadata && attributeData.AttributeClass?.ContainingNamespace?.ToDisplayString()?.StartsWith("System") != true)
        {
            sourceCodeWriter.Write("ClassMetadata = ");
            SourceInformationWriter.GenerateClassInformation(sourceCodeWriter, context, attributeData.AttributeClass!);
            sourceCodeWriter.Write(",");
        }

        // Add constructor arguments if available
        if (attributeData.ConstructorArguments.Length > 0)
        {
            sourceCodeWriter.Write("ConstructorArguments = new object?[]");
            sourceCodeWriter.Write("{");
            for (var i = 0; i < attributeData.ConstructorArguments.Length; i++)
            {
                if (i > 0) sourceCodeWriter.Write(",");
                sourceCodeWriter.Write(TypedConstantParser.GetRawTypedConstantValue(attributeData.ConstructorArguments[i]));
            }
            sourceCodeWriter.Write("}");
            sourceCodeWriter.Write(",");
        }

        // Add named arguments if available
        if (attributeData.NamedArguments.Length > 0)
        {
            sourceCodeWriter.Write("NamedArguments = new global::System.Collections.Generic.Dictionary<string, object?>()");
            sourceCodeWriter.Write("{");
            var first = true;
            foreach (var namedArg in attributeData.NamedArguments)
            {
                if (!first) sourceCodeWriter.Write(",");
                first = false;
                sourceCodeWriter.Write($@"{{ ""{namedArg.Key}"", {TypedConstantParser.GetRawTypedConstantValue(namedArg.Value)} }}");
            }
            sourceCodeWriter.Write("}");
            sourceCodeWriter.Write(",");
        }

        sourceCodeWriter.Write("}");
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

            sourceCodeWriter.Write(AsyncDataSourceGeneratorContainer.GetPropertyAssignmentFromAsyncDataSourceGeneratorAttribute(innerAttribute, context, attributeData.AttributeClass!, propertySymbol, sourceCodeWriter.TabLevel, true));
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
    
    // Write test attributes with special filtering and formatting
    public static void WriteTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        ImmutableArray<AttributeData> attributeDatas, string targetElement, string? targetMemberName = null, ITypeSymbol? targetType = null)
    {
        // Filter out attributes that don't have application syntax reference (except mscorlib)
        var filteredAttributes = attributeDatas.Where(x => 
            x.ApplicationSyntaxReference is not null || 
            x.AttributeClass?.ContainingAssembly?.Identity.Name == "mscorlib").ToImmutableArray();
        
        var targetTypeName = targetType?.GloballyQualified();
        
        // Use the enhanced WriteAttributeMetadatas with ClassMetadata support
        WriteAttributeMetadatas(sourceCodeWriter, context, filteredAttributes, targetElement, targetMemberName, targetTypeName, includeClassMetadata: true);
    }
    
    // Helper methods for different contexts (previously in TestAttributeWriter)
    public static void WriteAssemblyTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        IAssemblySymbol assembly)
    {
        var attributes = assembly.GetAttributes();
        WriteTestAttributes(sourceCodeWriter, context, attributes, "Assembly", assembly.Name);
    }

    public static void WriteTypeTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        ITypeSymbol type)
    {
        var attributes = type.GetAttributes();
        WriteTestAttributes(sourceCodeWriter, context, attributes, "Class", type.Name, type);
    }

    public static void WriteMethodTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        IMethodSymbol method)
    {
        var attributes = method.GetAttributes();
        WriteTestAttributes(sourceCodeWriter, context, attributes, "Method", method.Name, method.ContainingType);
    }

    public static void WritePropertyTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        IPropertySymbol property)
    {
        var attributes = property.GetAttributes();
        WriteTestAttributes(sourceCodeWriter, context, attributes, "Property", property.Name, property.ContainingType);
    }

    public static void WriteParameterTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        IParameterSymbol parameter)
    {
        var attributes = parameter.GetAttributes();
        WriteTestAttributes(sourceCodeWriter, context, attributes, "Parameter", parameter.Name, parameter.ContainingType);
    }
}
