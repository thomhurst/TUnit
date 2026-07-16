using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public class AttributeWriter(Compilation compilation)
{
    private const string TUnitRootNamespace = "TUnit";

    private readonly Dictionary<AttributeData, string> _attributeObjectInitializerCache = new();
    private readonly Dictionary<INamedTypeSymbol, bool> _tunitRelatedCache = new(SymbolEqualityComparer.Default);

    public void WriteAttributes(ICodeWriter sourceCodeWriter,
        IEnumerable<AttributeData> attributeDatas)
    {
        var first = true;

        // Filter out attributes that we can write
        foreach (var attributeData in attributeDatas)
        {
            if (attributeData.AttributeClass is null)
            {
                continue;
            }

            // Only include attributes that are, inherit from, or implement a TUnit type
            if (!IsTUnitRelatedAttribute(attributeData.AttributeClass))
            {
                continue;
            }

            // Skip attributes with compiler-generated type arguments
            if (attributeData.ConstructorArguments.Any(arg =>
                    arg is { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeSymbol } &&
                    typeSymbol.IsCompilerGeneratedType()))
            {
                continue;
            }

            if (!first)
            {
                sourceCodeWriter.AppendLine(",");
            }

            WriteAttribute(sourceCodeWriter, attributeData);
            first = false;
        }
    }

    public void WriteAttribute(ICodeWriter sourceCodeWriter, AttributeData attributeData)
    {
        if (attributeData.ApplicationSyntaxReference is null)
        {
            // For attributes from other assemblies (like inherited methods),
            // use the WriteAttributeWithoutSyntax approach
            WriteAttributeWithoutSyntax(sourceCodeWriter, attributeData);
        }
        else
        {
            // For attributes from the current compilation, use the syntax-based approach
            sourceCodeWriter.Append(GetAttributeObjectInitializer(attributeData));
        }
    }

    public string GetAttributeObjectInitializer(AttributeData attributeData)
    {
        if (_attributeObjectInitializerCache.TryGetValue(attributeData, out var initializer))
        {
            return initializer;
        }

        initializer = GetAttributeObjectInitializerInner(compilation, attributeData);
        _attributeObjectInitializerCache.Add(attributeData, initializer);
        return initializer;
    }

    private static string GetAttributeObjectInitializerInner(Compilation compilation, AttributeData attributeData)
    {
        var sourceCodeWriter = new CodeWriter("", includeHeader: false);

        var syntax = attributeData.ApplicationSyntaxReference?.GetSyntax();

        if (syntax is null)
        {
            WriteAttributeWithoutSyntax(sourceCodeWriter, attributeData);
            return sourceCodeWriter.ToString();
        }

        var arguments = syntax.ChildNodes()
            .OfType<AttributeArgumentListSyntax>()
            .FirstOrDefault()
            ?.Arguments ?? [];

        var properties = arguments.Where(x => x.NameEquals != null);

        var constructorArgs = arguments.Where(x => x.NameEquals == null);

        var attributeName = attributeData.AttributeClass!.GloballyQualified();

        var formattedConstructorArgs = string.Join(", ", constructorArgs.Select(x => FormatConstructorArgument(compilation, x)));

        var formattedProperties = properties.Select(x => FormatProperty(compilation, x)).ToArray();

        sourceCodeWriter.Append($"new {attributeName}({formattedConstructorArgs})");

        // Only add object initializer if we have regular properties to set
        // Don't include data source properties - they'll be handled by property injection
        if (formattedProperties.Length == 0)
        {
            return sourceCodeWriter.ToString();
        }

        sourceCodeWriter.AppendLine();
        sourceCodeWriter.Append("{");
        foreach (var property in formattedProperties)
        {
            sourceCodeWriter.Append($"{property},");
        }

        sourceCodeWriter.Append("}");

        return sourceCodeWriter.ToString();
    }

    private static string FormatConstructorArgument(Compilation compilation, AttributeArgumentSyntax attributeArgumentSyntax)
    {
        if (attributeArgumentSyntax.NameColon is not null)
        {
            return $"{attributeArgumentSyntax.NameColon!.Name}: {attributeArgumentSyntax.Expression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(compilation.GetSemanticModel(attributeArgumentSyntax.SyntaxTree)))!.ToFullString()}";
        }

        return attributeArgumentSyntax.Accept(new FullyQualifiedWithGlobalPrefixRewriter(compilation.GetSemanticModel(attributeArgumentSyntax.SyntaxTree)))!.ToFullString();
    }

    private static string FormatProperty(Compilation compilation, AttributeArgumentSyntax attributeArgumentSyntax)
    {
        return $"{attributeArgumentSyntax.NameEquals!.Name} = {attributeArgumentSyntax.Expression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(compilation.GetSemanticModel(attributeArgumentSyntax.SyntaxTree)))!.ToFullString()}";
    }

    public static void WriteAttributeWithoutSyntax(ICodeWriter sourceCodeWriter, AttributeData attributeData)
    {
        var attributeName = attributeData.AttributeClass!.GloballyQualified();

        // Skip if any constructor arguments contain compiler-generated types
        if (attributeData.ConstructorArguments.Any(arg =>
            arg is { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeSymbol } &&
            typeSymbol.IsCompilerGeneratedType()))
        {
            return;
        }

        var constructorArgs = attributeData.ConstructorArguments.Select(arg => TypedConstantParser.GetRawTypedConstantValue(arg));
        var formattedConstructorArgs = string.Join(", ", constructorArgs);

        var namedArgs = attributeData.NamedArguments.Select(arg => $"{arg.Key} = {TypedConstantParser.GetRawTypedConstantValue(arg.Value)}");
        var formattedNamedArgs = string.Join(", ", namedArgs);

        sourceCodeWriter.Append($"new {attributeName}({formattedConstructorArgs})");

        // Check if we need to add properties (named arguments only, not data source properties)
        var hasNamedArgs = !string.IsNullOrEmpty(formattedNamedArgs);

        if (!hasNamedArgs)
        {
            return;
        }

        sourceCodeWriter.AppendLine();
        sourceCodeWriter.Append("{");

        if (hasNamedArgs)
        {
            sourceCodeWriter.Append($"{formattedNamedArgs}");
        }

        sourceCodeWriter.Append("}");
    }


    private bool IsTUnitRelatedAttribute(INamedTypeSymbol attributeClass)
    {
        if (_tunitRelatedCache.TryGetValue(attributeClass, out var cached))
        {
            return cached;
        }

        var result = attributeClass.GetSelfAndBaseTypes().Any(IsInTUnitNamespace)
                     || attributeClass.AllInterfaces.Any(IsInTUnitNamespace);

        _tunitRelatedCache[attributeClass] = result;
        return result;
    }

    private static bool IsInTUnitNamespace(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace;
        INamespaceSymbol? outermost = null;

        while (ns is { IsGlobalNamespace: false })
        {
            outermost = ns;
            ns = ns.ContainingNamespace;
        }

        return outermost?.Name == TUnitRootNamespace;
    }

}
