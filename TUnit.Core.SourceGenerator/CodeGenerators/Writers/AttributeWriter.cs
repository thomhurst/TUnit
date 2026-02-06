using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public class AttributeWriter(Compilation compilation, TUnit.Core.SourceGenerator.Helpers.WellKnownTypes wellKnownTypes)
{
    private readonly Dictionary<AttributeData, string> _attributeObjectInitializerCache = new();

    public void WriteAttributes(ICodeWriter sourceCodeWriter,
        IEnumerable<AttributeData> attributeDatas)
    {
        var first = true;

        // Filter out attributes that we can write
        foreach (var attributeData in attributeDatas)
        {
            // Include attributes with syntax reference (from current compilation)
            // Include attributes without syntax reference (from other assemblies) as long as they have an AttributeClass
            if (attributeData.ApplicationSyntaxReference is null && attributeData.AttributeClass is null)
            {
                continue;
            }

            // Skip compiler-internal and assembly-level attributes
            if (ShouldSkipCompilerInternalAttribute(attributeData))
            {
                continue;
            }

            // Skip framework-specific attributes when targeting older frameworks
            // We determine this by checking if we can compile the attribute
            if (ShouldSkipFrameworkSpecificAttribute(attributeData))
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


    private bool ShouldSkipFrameworkSpecificAttribute(AttributeData attributeData)
    {
        if (attributeData.AttributeClass == null)
        {
            return false;
        }

        // Generic approach: Check if the attribute type is actually available in the target compilation
        // This works by seeing if we can resolve the type from the compilation's references
        var fullyQualifiedName = wellKnownTypes.GetDisplayString(attributeData.AttributeClass);

        // Check if this is a system/runtime attribute that might not exist on all frameworks
        if (fullyQualifiedName.StartsWith("System.") || fullyQualifiedName.StartsWith("Microsoft."))
        {
            // Try to get the type from the compilation
            // If it doesn't exist in the compilation's references, we should skip it
            var typeSymbol = wellKnownTypes.TryGet(fullyQualifiedName);

            // If the type doesn't exist in the compilation, skip it
            if (typeSymbol == null)
            {
                return true;
            }

            // Special handling for attributes that exist but may not be usable
            // For example, nullable attributes exist in the reference assemblies but not at runtime for .NET Framework
            if (IsNullableAttribute(fullyQualifiedName))
            {
                // Check if we're targeting .NET Framework by looking at references
                var isNetFramework = compilation.References.Any(r =>
                    r.Display?.Contains("mscorlib") == true &&
                    !r.Display.Contains("System.Runtime"));

                if (isNetFramework)
                {
                    return true; // Skip nullable attributes on .NET Framework
                }
            }
        }

        return false;
    }

    private static bool IsNullableAttribute(string fullyQualifiedName)
    {
        return fullyQualifiedName.Contains("NullableAttribute") ||
               fullyQualifiedName.Contains("NullableContextAttribute") ||
               fullyQualifiedName.Contains("NullablePublicOnlyAttribute");
    }

    private bool ShouldSkipCompilerInternalAttribute(AttributeData attributeData)
    {
        if (attributeData.AttributeClass == null)
        {
            return false;
        }

        var fullyQualifiedName = wellKnownTypes.GetDisplayString(attributeData.AttributeClass);

        // Skip compiler-internal attributes that should never be re-emitted
        // System.Runtime.CompilerServices contains compiler-generated and structural metadata attributes
        if (fullyQualifiedName.StartsWith("System.Runtime.CompilerServices."))
        {
            return true;
        }

        // Skip debugger attributes (compiler-generated for debugging support)
        if (fullyQualifiedName.StartsWith("System.Diagnostics.Debugger"))
        {
            return true;
        }

        // Skip ParamArrayAttribute (compiler-generated for params keyword)
        return fullyQualifiedName == "System.ParamArrayAttribute";
    }

}
