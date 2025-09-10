using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public class AttributeWriter
{
    public static void WriteAttributes(ICodeWriter sourceCodeWriter, Compilation compilation,
        ImmutableArray<AttributeData> attributeDatas)
    {
        var attributesToWrite = new List<AttributeData>();

        // Filter out attributes that we can write
        foreach (var attributeData in attributeDatas)
        {
            // Include attributes with syntax reference (from current compilation)
            // Include attributes without syntax reference (from other assemblies) as long as they have an AttributeClass
            if (attributeData.ApplicationSyntaxReference is not null || attributeData.AttributeClass is not null)
            {
                // Skip framework-specific attributes when targeting older frameworks
                // We determine this by checking if we can compile the attribute
                if (ShouldSkipFrameworkSpecificAttribute(compilation, attributeData))
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

                attributesToWrite.Add(attributeData);
            }
        }

        for (var index = 0; index < attributesToWrite.Count; index++)
        {
            var attributeData = attributesToWrite[index];

            WriteAttribute(sourceCodeWriter, compilation, attributeData);

            if (index != attributesToWrite.Count - 1)
            {
                sourceCodeWriter.AppendLine(",");
            }
        }
    }

    public static void WriteAttribute(ICodeWriter sourceCodeWriter, Compilation compilation,
        AttributeData attributeData)
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
            sourceCodeWriter.Append(GetAttributeObjectInitializer(compilation, attributeData));
        }
    }

    public static void WriteAttributeMetadata(ICodeWriter sourceCodeWriter, Compilation compilation,
        AttributeData attributeData, string targetElement, string? targetMemberName, string? targetTypeName, bool includeClassMetadata = false)
    {
        sourceCodeWriter.Append("new global::TUnit.Core.AttributeMetadata");
        sourceCodeWriter.Append(" { ");
        sourceCodeWriter.Append($"Instance = {GetAttributeObjectInitializer(compilation, attributeData)}, ");
        sourceCodeWriter.Append($"TargetElement = global::TUnit.Core.TestAttributeTarget.{targetElement}, ");

        if (targetMemberName != null)
        {
            sourceCodeWriter.Append($"TargetMemberName = \"{targetMemberName}\", ");
        }

        if (targetTypeName != null)
        {
            sourceCodeWriter.Append($"TargetType = typeof({targetTypeName}), ");
        }

        // Add ClassMetadata if requested and not a system attribute
        if (includeClassMetadata && attributeData.AttributeClass?.ContainingNamespace?.ToDisplayString()?.StartsWith("System") != true)
        {
            sourceCodeWriter.Append("ClassMetadata = ");
            SourceInformationWriter.GenerateClassInformation(sourceCodeWriter, compilation, attributeData.AttributeClass!);
            sourceCodeWriter.Append(", ");
        }

        if (attributeData.ConstructorArguments.Length > 0)
        {
            sourceCodeWriter.Append("ConstructorArguments = new object?[] { ");

            foreach (var typedConstant in attributeData.ConstructorArguments)
            {
                sourceCodeWriter.Append($"{TypedConstantParser.GetRawTypedConstantValue(typedConstant)}, ");
            }

            sourceCodeWriter.Append("}, ");
        }

        if (attributeData.NamedArguments.Length > 0)
        {
            sourceCodeWriter.Append("NamedArguments = new global::System.Collections.Generic.Dictionary<string, object?>() { ");
            foreach (var namedArg in attributeData.NamedArguments)
            {
                sourceCodeWriter.Append($"[\"{namedArg.Key}\"] = {TypedConstantParser.GetRawTypedConstantValue(namedArg.Value)}, ");
            }
            sourceCodeWriter.Append("}, ");
        }

        sourceCodeWriter.Append("}");
    }

    public static string GetAttributeObjectInitializer(Compilation compilation,
        AttributeData attributeData)
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

        if (formattedProperties.Length == 0
            && !HasNestedDataGeneratorProperties(attributeData))
        {
            return sourceCodeWriter.ToString();
        }

        sourceCodeWriter.AppendLine();
        sourceCodeWriter.Append("{");
        foreach (var property in formattedProperties)
        {
            sourceCodeWriter.Append($"{property},");
        }

        WriteDataSourceGeneratorProperties(sourceCodeWriter, compilation, attributeData);

        sourceCodeWriter.Append("}");

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

    private static void WriteDataSourceGeneratorProperties(ICodeWriter sourceCodeWriter, Compilation compilation, AttributeData attributeData)
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

            sourceCodeWriter.Append($"{propertySymbol.Name} = ");

            var propertyType = propertySymbol.Type.GloballyQualified();
            var isNullable = propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated;

            if (propertySymbol.Type.IsReferenceType && !isNullable)
            {
                sourceCodeWriter.Append("null!,");
            }
            else if (propertySymbol.Type.IsValueType && !isNullable)
            {
                sourceCodeWriter.Append($"default({propertyType}),");
            }
            else
            {
                sourceCodeWriter.Append("null,");
            }
        }
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

        var constructorArgs = attributeData.ConstructorArguments.Select(TypedConstantParser.GetRawTypedConstantValue);
        var formattedConstructorArgs = string.Join(", ", constructorArgs);

        var namedArgs = attributeData.NamedArguments.Select(arg => $"{arg.Key} = {TypedConstantParser.GetRawTypedConstantValue(arg.Value)}");
        var formattedNamedArgs = string.Join(", ", namedArgs);

        sourceCodeWriter.Append($"new {attributeName}({formattedConstructorArgs})");

        // Check if we need to add properties (named arguments or data generator properties)
        var hasNamedArgs = !string.IsNullOrEmpty(formattedNamedArgs);
        var hasDataGeneratorProperties = HasNestedDataGeneratorProperties(attributeData);

        if (!hasNamedArgs && !hasDataGeneratorProperties)
        {
            return;
        }

        sourceCodeWriter.AppendLine();
        sourceCodeWriter.Append("{");

        if (hasNamedArgs)
        {
            sourceCodeWriter.Append($"{formattedNamedArgs}");
            if (hasDataGeneratorProperties)
            {
                sourceCodeWriter.Append(",");
            }
        }

        if (hasDataGeneratorProperties)
        {
            // For attributes without syntax, we still need to handle data generator properties
            // but we can't rely on syntax analysis, so we'll use a simpler approach
            WriteDataSourceGeneratorPropertiesWithoutSyntax(sourceCodeWriter, attributeData);
        }

        sourceCodeWriter.Append("}");
    }

    private static void WriteDataSourceGeneratorPropertiesWithoutSyntax(ICodeWriter sourceCodeWriter, AttributeData attributeData)
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

            sourceCodeWriter.Append($"{propertySymbol.Name} = ");

            var propertyType = propertySymbol.Type.GloballyQualified();
            var isNullable = propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated;

            if (propertySymbol.Type.IsReferenceType && !isNullable)
            {
                sourceCodeWriter.Append("null!,");
            }
            else if (propertySymbol.Type.IsValueType && !isNullable)
            {
                sourceCodeWriter.Append($"default({propertyType}),");
            }
            else
            {
                sourceCodeWriter.Append("null,");
            }
        }
    }

    private static bool ShouldSkipFrameworkSpecificAttribute(Compilation compilation, AttributeData attributeData)
    {
        if (attributeData.AttributeClass == null)
        {
            return false;
        }

        // Generic approach: Check if the attribute type is actually available in the target compilation
        // This works by seeing if we can resolve the type from the compilation's references
        var fullyQualifiedName = attributeData.AttributeClass.ToDisplayString();

        // Check if this is a system/runtime attribute that might not exist on all frameworks
        if (fullyQualifiedName.StartsWith("System.") || fullyQualifiedName.StartsWith("Microsoft."))
        {
            // Try to get the type from the compilation
            // If it doesn't exist in the compilation's references, we should skip it
            var typeSymbol = compilation.GetTypeByMetadataName(fullyQualifiedName);

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

}
