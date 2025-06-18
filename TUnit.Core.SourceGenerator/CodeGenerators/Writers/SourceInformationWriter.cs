using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class SourceInformationWriter
{
    public static void GenerateClassInformation(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol)
    {
        sourceCodeWriter.Write($"global::TUnit.Core.ClassMetadata.GetOrAdd(\"{namedTypeSymbol.GloballyQualified()}\", () => new global::TUnit.Core.ClassMetadata");
        sourceCodeWriter.Write("{");

        var parent = namedTypeSymbol.ContainingType;

        if (parent != null)
        {
            sourceCodeWriter.Write("Parent = ");
            GenerateClassInformation(sourceCodeWriter, context, parent);
        }
        else
        {
            sourceCodeWriter.Write("Parent = null,");
        }

        sourceCodeWriter.Write($"Type = typeof({namedTypeSymbol.GloballyQualified()}),");

        sourceCodeWriter.Write("Assembly = ");
        GenerateAssemblyInformation(sourceCodeWriter, context, namedTypeSymbol.ContainingAssembly);

        sourceCodeWriter.Write($"Name = \"{namedTypeSymbol.Name}\",");
        sourceCodeWriter.Write($"Namespace = \"{namedTypeSymbol.ContainingNamespace.ToDisplayString()}\",");

        sourceCodeWriter.Write("Attributes = ");
        AttributeWriter.WriteAttributes(sourceCodeWriter, context, namedTypeSymbol.GetSelfAndBaseTypes().SelectMany(type => type.GetAttributes()).ToImmutableArray());

        sourceCodeWriter.Write("Parameters = ");
        var parameters = namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters
            ?? ImmutableArray<IParameterSymbol>.Empty;

        if (parameters.Length == 0)
        {
            sourceCodeWriter.Write("[],");
        }
        else
        {
            sourceCodeWriter.Write("[");

            foreach (var parameter in parameters)
            {
                    GenerateParameterInformation(sourceCodeWriter, context, parameter, ArgumentsType.ClassConstructor,
                    null);
            }

            sourceCodeWriter.Write("],");
        }

        sourceCodeWriter.Write("Properties = ");
        var properties = namedTypeSymbol.GetMembersIncludingBase().OfType<IPropertySymbol>().ToArray();

        if(properties.Length == 0)
        {
            sourceCodeWriter.Write("[],");
        }
        else
        {
            sourceCodeWriter.Write("[");

            foreach (var propertySymbol in properties.Where(x => x.DeclaredAccessibility == Accessibility.Public))
            {
                GeneratePropertyInformation(sourceCodeWriter, context, propertySymbol, namedTypeSymbol);
            }

            sourceCodeWriter.Write("],");
        }

        sourceCodeWriter.Write("}),");
    }

    private static void GenerateAssemblyInformation(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, IAssemblySymbol assembly)
    {
        sourceCodeWriter.Write(
            $"global::TUnit.Core.AssemblyMetadata.GetOrAdd(\"{assembly.Name}\", () => new global::TUnit.Core.AssemblyMetadata");
        sourceCodeWriter.Write("{");
        sourceCodeWriter.Write($"Name = \"{assembly.Name}\",");

        sourceCodeWriter.Write("Attributes = ");
        AttributeWriter.WriteAttributes(sourceCodeWriter, context, assembly.GetAttributes());

        sourceCodeWriter.Write("}),");
    }

    public static void GenerateMethodInformation(SourceCodeWriter sourceCodeWriter,
        GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol, IMethodSymbol methodSymbol,
        IDictionary<string, string>? genericSubstitutions, char suffix)
    {
        sourceCodeWriter.Write("new global::TUnit.Core.MethodMetadata");
        sourceCodeWriter.Write("{");
        sourceCodeWriter.Write($"Type = typeof({namedTypeSymbol.GloballyQualified()}),");
        sourceCodeWriter.Write($"Name = \"{methodSymbol.Name}\",");
        sourceCodeWriter.Write($"GenericTypeCount = {methodSymbol.TypeParameters.Length},");
        sourceCodeWriter.Write($"ReturnType = typeof({methodSymbol.ReturnType.GloballyQualified()}),");

        sourceCodeWriter.Write("Attributes = ");
        AttributeWriter.WriteAttributes(sourceCodeWriter, context, methodSymbol.GetAttributes());

        sourceCodeWriter.Write("Parameters = ");
        var parameters = methodSymbol.Parameters;

        if (parameters.Length == 0)
        {
            sourceCodeWriter.Write("[],");
        }
        else
        {
            sourceCodeWriter.Write("[");

            foreach (var parameter in parameters)
            {
                    GenerateParameterInformation(sourceCodeWriter, context, parameter, ArgumentsType.ClassConstructor,
                    null);
            }

            sourceCodeWriter.Write("],");
        }

        sourceCodeWriter.Write("Class = ");
        GenerateClassInformation(sourceCodeWriter, context, namedTypeSymbol);

        sourceCodeWriter.Write("}");
        sourceCodeWriter.Write($"{suffix}");
        sourceCodeWriter.WriteLine();
    }

    public static void GenerateMembers(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol, ImmutableArray<IParameterSymbol> parameters, IPropertySymbol? property, ArgumentsType argumentsType)
    {
        if(parameters.Length == 0 && property is null)
        {
            sourceCodeWriter.Write("[],");
            return;
        }

        sourceCodeWriter.Write("[");

        if (property is not null)
        {
            GeneratePropertyInformation(sourceCodeWriter, context, property, namedTypeSymbol);
        }

        foreach (var parameter in parameters)
        {
            GenerateParameterInformation(sourceCodeWriter, context, parameter, argumentsType, null);
        }

        sourceCodeWriter.Write("],");
    }

    public static void GeneratePropertyInformation(SourceCodeWriter sourceCodeWriter,
        GeneratorAttributeSyntaxContext context, IPropertySymbol property, INamedTypeSymbol namedTypeSymbol)
    {
        sourceCodeWriter.Write("new global::TUnit.Core.PropertyMetadata");
        sourceCodeWriter.Write("{");
        sourceCodeWriter.Write($"ReflectionInfo = typeof({namedTypeSymbol.GloballyQualified()}).GetProperty(\"{property.Name}\"),");
        sourceCodeWriter.Write($"Type = typeof({property.Type.GloballyQualified()}),");
        sourceCodeWriter.Write($"Name = \"{property.Name}\",");
        sourceCodeWriter.Write($"IsStatic = {property.IsStatic.ToString().ToLower()},");
        sourceCodeWriter.Write($"Getter = {GetPropertyAccessor(namedTypeSymbol, property)},");

        sourceCodeWriter.Write("Attributes = ");
        AttributeWriter.WriteAttributes(sourceCodeWriter, context, property.GetAttributes());
        
        // For now, always set ClassMetadata to null to avoid circular references
        // The ClassMetadata will be available through the cache if needed at runtime
        sourceCodeWriter.Write("ClassMetadata = null,");

        sourceCodeWriter.Write("},");
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

    public static void GenerateParameterInformation(SourceCodeWriter sourceCodeWriter,
        GeneratorAttributeSyntaxContext context,
        IParameterSymbol parameter, ArgumentsType argumentsType,
        IDictionary<string, string>? genericSubstitutions)
    {
        var type = parameter.Type.GloballyQualified();

        if (parameter.Type.IsGenericDefinition())
        {
            type = GetTypeOrSubstitution(parameter.Type);
        }

        sourceCodeWriter.Write($"new global::TUnit.Core.ParameterMetadata<{type}>");
        sourceCodeWriter.Write("{");
        sourceCodeWriter.Write($"Name = \"{parameter.Name}\",");

        sourceCodeWriter.Write("Attributes = ");
        AttributeWriter.WriteAttributes(sourceCodeWriter, context, parameter.GetAttributes());

        // TODO: Struggling to get this to work with generic type parameters
        sourceCodeWriter.Write("ReflectionInfo = null!,");

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

        sourceCodeWriter.Write("},");

        string GetTypeOrSubstitution(ITypeSymbol type)
        {
            return genericSubstitutions?.TryGetValue(type.GloballyQualified(), out var substitution) == true
                ? substitution
                // We can't find the generic type - Fall back to object
                : "object";
        }
    }
}
