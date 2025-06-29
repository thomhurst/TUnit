using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class SourceInformationWriter
{
    public static void GenerateClassInformation(ICodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol)
    {
        sourceCodeWriter.Append($"global::TUnit.Core.ClassMetadata.GetOrAdd(\"{namedTypeSymbol.GloballyQualified()}\", () => new global::TUnit.Core.ClassMetadata");
        sourceCodeWriter.Append("{");

        var parent = namedTypeSymbol.ContainingType;

        if (parent != null)
        {
            sourceCodeWriter.Append("Parent = ");
            GenerateClassInformation(sourceCodeWriter, context, parent);
        }
        else
        {
            sourceCodeWriter.Append("Parent = null,");
        }

        sourceCodeWriter.Append($"Type = typeof({namedTypeSymbol.GloballyQualified()}),");
        sourceCodeWriter.Append($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(namedTypeSymbol)},");

        sourceCodeWriter.Append("Assembly = ");
        GenerateAssemblyInformation(sourceCodeWriter, context, namedTypeSymbol.ContainingAssembly);

        sourceCodeWriter.Append($"Name = \"{namedTypeSymbol.Name}\",");
        sourceCodeWriter.Append($"Namespace = \"{namedTypeSymbol.ContainingNamespace.ToDisplayString()}\",");

        sourceCodeWriter.Append("Attributes = ");
        AttributeWriter.WriteAttributeMetadatas(sourceCodeWriter, context, namedTypeSymbol.GetSelfAndBaseTypes().SelectMany(type => type.GetAttributes()).ToImmutableArray(), "Class", namedTypeSymbol.Name, namedTypeSymbol.ToDisplayString());

        sourceCodeWriter.Append("Parameters = ");
        var parameters = namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters
            ?? ImmutableArray<IParameterSymbol>.Empty;

        if (parameters.Length == 0)
        {
            sourceCodeWriter.Append("[],");
        }
        else
        {
            sourceCodeWriter.Append("[");

            foreach (var parameter in parameters)
            {
                GenerateParameterInformation(sourceCodeWriter, context, parameter, ArgumentsType.ClassConstructor,
                null);
            }

            sourceCodeWriter.Append("],");
        }

        sourceCodeWriter.Append("Properties = ");
        var properties = namedTypeSymbol.GetMembersIncludingBase().OfType<IPropertySymbol>().ToArray();

        if (properties.Length == 0)
        {
            sourceCodeWriter.Append("[],");
        }
        else
        {
            sourceCodeWriter.Append("[");

            foreach (var propertySymbol in properties.Where(x => x.DeclaredAccessibility == Accessibility.Public))
            {
                GeneratePropertyInformation(sourceCodeWriter, context, propertySymbol, namedTypeSymbol);
            }

            sourceCodeWriter.Append("],");
        }

        sourceCodeWriter.Append("}),");
    }

    private static void GenerateAssemblyInformation(ICodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, IAssemblySymbol assembly)
    {
        sourceCodeWriter.Append(
            $"global::TUnit.Core.AssemblyMetadata.GetOrAdd(\"{assembly.Name}\", () => new global::TUnit.Core.AssemblyMetadata");
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append($"Name = \"{assembly.Name}\",");

        sourceCodeWriter.Append("Attributes = ");
        AttributeWriter.WriteAttributeMetadatas(sourceCodeWriter, context, assembly.GetAttributes(), "Assembly", assembly.Name);

        sourceCodeWriter.Append("}),");
    }

    public static void GenerateMethodInformation(ICodeWriter sourceCodeWriter,
        GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol, IMethodSymbol methodSymbol,
        IDictionary<string, string>? genericSubstitutions, char suffix)
    {
        sourceCodeWriter.Append("new global::TUnit.Core.MethodMetadata");
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append($"Type = typeof({namedTypeSymbol.GloballyQualified()}),");
        sourceCodeWriter.Append($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(namedTypeSymbol)},");
        sourceCodeWriter.Append($"Name = \"{methodSymbol.Name}\",");
        sourceCodeWriter.Append($"GenericTypeCount = {methodSymbol.TypeParameters.Length},");
        sourceCodeWriter.Append($"ReturnType = typeof({methodSymbol.ReturnType.GloballyQualified()}),");
        sourceCodeWriter.Append($"ReturnTypeReference = {CodeGenerationHelpers.GenerateTypeReference(methodSymbol.ReturnType)},");

        sourceCodeWriter.Append("Attributes = ");
        AttributeWriter.WriteAttributeMetadatas(sourceCodeWriter, context, methodSymbol.GetAttributes(), "Method", methodSymbol.Name, namedTypeSymbol.ToDisplayString());

        sourceCodeWriter.Append("Parameters = ");
        var parameters = methodSymbol.Parameters;

        if (parameters.Length == 0)
        {
            sourceCodeWriter.Append("[],");
        }
        else
        {
            sourceCodeWriter.Append("[");

            foreach (var parameter in parameters)
            {
                GenerateParameterInformation(sourceCodeWriter, context, parameter, ArgumentsType.ClassConstructor,
                null);
            }

            sourceCodeWriter.Append("],");
        }

        sourceCodeWriter.Append("Class = ");
        GenerateClassInformation(sourceCodeWriter, context, namedTypeSymbol);

        sourceCodeWriter.Append("}");
        sourceCodeWriter.Append($"{suffix}");
        sourceCodeWriter.AppendLine();
    }

    public static void GenerateMembers(ICodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol, ImmutableArray<IParameterSymbol> parameters, IPropertySymbol? property, ArgumentsType argumentsType)
    {
        if (parameters.Length == 0 && property is null)
        {
            sourceCodeWriter.Append("[],");
            return;
        }

        sourceCodeWriter.Append("[");

        if (property is not null)
        {
            GeneratePropertyInformation(sourceCodeWriter, context, property, namedTypeSymbol);
        }

        foreach (var parameter in parameters)
        {
            GenerateParameterInformation(sourceCodeWriter, context, parameter, argumentsType, null);
        }

        sourceCodeWriter.Append("],");
    }

    public static void GeneratePropertyInformation(ICodeWriter sourceCodeWriter,
        GeneratorAttributeSyntaxContext context, IPropertySymbol property, INamedTypeSymbol namedTypeSymbol)
    {
        sourceCodeWriter.Append("new global::TUnit.Core.PropertyMetadata");
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append($"ReflectionInfo = typeof({namedTypeSymbol.GloballyQualified()}).GetProperty(\"{property.Name}\"),");
        sourceCodeWriter.Append($"Type = typeof({property.Type.GloballyQualified()}),");
        sourceCodeWriter.Append($"Name = \"{property.Name}\",");
        sourceCodeWriter.Append($"IsStatic = {property.IsStatic.ToString().ToLower()},");
        sourceCodeWriter.Append($"Getter = {GetPropertyAccessor(namedTypeSymbol, property)},");

        sourceCodeWriter.Append("Attributes = ");
        AttributeWriter.WriteAttributeMetadatas(sourceCodeWriter, context, property.GetAttributes(), "Property", property.Name, namedTypeSymbol.ToDisplayString());

        // For now, always set ClassMetadata to null to avoid circular references
        // The ClassMetadata will be available through the cache if needed at runtime
        sourceCodeWriter.Append("ClassMetadata = null,");

        sourceCodeWriter.Append("}");
        sourceCodeWriter.Append(",");
    }


    private static string GetPropertyAccessor(INamedTypeSymbol namedTypeSymbol, IPropertySymbol property)
    {
        return property.IsStatic
            ? $"_ => {namedTypeSymbol.GloballyQualified()}.{property.Name}"
            : $"o => (({namedTypeSymbol.GloballyQualified()})o).{property.Name}";
    }

    private static bool ShouldGenerateClassMetadataForPropertyType(GeneratorAttributeSyntaxContext context, IPropertySymbol property)
    {
        var compilation = context.SemanticModel.Compilation;
        var dataAttributeType = compilation.GetTypeByMetadataName("TUnit.Core.IDataAttribute");

        if (dataAttributeType == null)
        {
            return false;
        }

        // Check if the property type itself implements IDataAttribute
        if (property.Type is INamedTypeSymbol namedType &&
            namedType.AllInterfaces.Contains(dataAttributeType, SymbolEqualityComparer.Default))
        {
            return true;
        }

        // Check if any of the property's attributes implement IDataAttribute
        foreach (var attribute in property.GetAttributes())
        {
            if (attribute.AttributeClass != null &&
                attribute.AttributeClass.AllInterfaces.Contains(dataAttributeType, SymbolEqualityComparer.Default))
            {
                return true;
            }
        }

        return false;
    }

    public static void GenerateParameterInformation(ICodeWriter sourceCodeWriter,
        GeneratorAttributeSyntaxContext context,
        IParameterSymbol parameter, ArgumentsType argumentsType,
        IDictionary<string, string>? genericSubstitutions)
    {
        var type = parameter.Type.GloballyQualified();

        if (parameter.Type.IsGenericDefinition())
        {
            type = GetTypeOrSubstitution(parameter.Type);
        }

        sourceCodeWriter.Append($"new global::TUnit.Core.ParameterMetadata<{type}>");
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append($"Name = \"{parameter.Name}\",");
        sourceCodeWriter.Append($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(parameter.Type)},");

        sourceCodeWriter.Append("Attributes = ");
        var containingType = parameter.ContainingSymbol switch
        {
            IMethodSymbol method => method.ContainingType,
            IPropertySymbol property => property.ContainingType,
            _ => null
        };
        AttributeWriter.WriteAttributeMetadatas(sourceCodeWriter, context, parameter.GetAttributes(), "Parameter", parameter.Name, containingType?.ToDisplayString());

        sourceCodeWriter.Append("ReflectionInfo = null!,");

        // if(argumentsType == ArgumentsType.ClassConstructor)
        // {
        //     var methodSymbol = (IMethodSymbol)parameter.ContainingSymbol;
        //     var parameterTypesString = string.Join(", ", methodSymbol.Parameters.Select(p => $"typeof({GetTypeOrSubstitution(p.Type)})"));
        //     var containingType = methodSymbol.ContainingType.GloballyQualified();
        //     var parameterIndex = parameter.Ordinal;
        //
        //     sourceCodeWriter.WriteLine($"ReflectionInfo = global::TUnit.Core.Helpers.RobustParameterInfoRetriever.GetConstructorParameterInfo(typeof({containingType}), new Type[] {{{parameterTypesString}}}, {parameterIndex}, typeof({parameter.Type.GloballyQualified()}), \"{parameter.Name}\"),");
        // }
        //
        // if (argumentsType == ArgumentsType.Method)
        // {
        //     var methodSymbol = (IMethodSymbol)parameter.ContainingSymbol;
        //     var parameterTypesString = string.Join(", ", methodSymbol.Parameters.Select(p => $"typeof({GetTypeOrSubstitution(p.Type)})"));
        //     var containingType = parameter.ContainingSymbol.ContainingType.GloballyQualified();
        //     var methodName = parameter.ContainingSymbol.Name;
        //     var parameterIndex = parameter.Ordinal;
        //     var isStatic = methodSymbol.IsStatic;
        //     var genericParameterCount = methodSymbol.TypeParameters.Length;
        //
        //     sourceCodeWriter.WriteLine($"ReflectionInfo = global::TUnit.Core.Helpers.RobustParameterInfoRetriever.GetMethodParameterInfo(typeof({containingType}), \"{methodName}\", {parameterIndex}, new Type[] {{{parameterTypesString}}}, {isStatic.ToString().ToLowerInvariant()}, {genericParameterCount}),");
        // }

        sourceCodeWriter.Append("}");
        sourceCodeWriter.Append(",");

        string GetTypeOrSubstitution(ITypeSymbol type)
        {
            return genericSubstitutions?.TryGetValue(type.GloballyQualified(), out var substitution) == true
                ? substitution
                // We can't find the generic type - Fall back to object
                : "object";
        }
    }
}
