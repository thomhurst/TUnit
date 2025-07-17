using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class SourceInformationWriter
{
    public static void GenerateClassInformation(ICodeWriter sourceCodeWriter, Compilation compilation, INamedTypeSymbol namedTypeSymbol)
    {
        sourceCodeWriter.Append($"global::TUnit.Core.ClassMetadata.GetOrAdd(\"{namedTypeSymbol.GloballyQualified()}\", () => new global::TUnit.Core.ClassMetadata");
        sourceCodeWriter.Append("{");

        var parent = namedTypeSymbol.ContainingType;

        if (parent != null)
        {
            sourceCodeWriter.Append("Parent = ");
            GenerateClassInformation(sourceCodeWriter, compilation, parent);
        }
        else
        {
            sourceCodeWriter.Append("Parent = null,");
        }

        var safeTypeName = CodeGenerationHelpers.ContainsTypeParameter(namedTypeSymbol) ? "object" : namedTypeSymbol.GloballyQualified();
        sourceCodeWriter.Append($"Type = typeof({safeTypeName}),");
        sourceCodeWriter.Append($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(namedTypeSymbol)},");

        sourceCodeWriter.Append("Assembly = ");
        GenerateAssemblyInformation(sourceCodeWriter, compilation, namedTypeSymbol.ContainingAssembly);

        sourceCodeWriter.Append($"Name = \"{namedTypeSymbol.Name}\",");
        sourceCodeWriter.Append($"Namespace = \"{namedTypeSymbol.ContainingNamespace.ToDisplayString()}\",");


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
                GenerateParameterInformation(sourceCodeWriter, compilation, parameter, ArgumentsType.ClassConstructor,
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
                GeneratePropertyInformation(sourceCodeWriter, compilation, propertySymbol, namedTypeSymbol);
            }

            sourceCodeWriter.Append("],");
        }

        sourceCodeWriter.Append("}),");
    }

    private static void GenerateAssemblyInformation(ICodeWriter sourceCodeWriter, Compilation compilation, IAssemblySymbol assembly)
    {
        sourceCodeWriter.Append(
            $"global::TUnit.Core.AssemblyMetadata.GetOrAdd(\"{assembly.Name}\", () => new global::TUnit.Core.AssemblyMetadata");
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append($"Name = \"{assembly.Name}\",");

        sourceCodeWriter.Append("}),");
    }

    public static void GenerateMethodInformation(ICodeWriter sourceCodeWriter,
        Compilation compilation, INamedTypeSymbol namedTypeSymbol, IMethodSymbol methodSymbol,
        IDictionary<string, string>? genericSubstitutions, char suffix)
    {
        sourceCodeWriter.Append("new global::TUnit.Core.MethodMetadata");
        sourceCodeWriter.Append("{");
        var safeTypeName = CodeGenerationHelpers.ContainsTypeParameter(namedTypeSymbol) ? "object" : namedTypeSymbol.GloballyQualified();
        sourceCodeWriter.Append($"Type = typeof({safeTypeName}),");
        sourceCodeWriter.Append($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(namedTypeSymbol)},");
        sourceCodeWriter.Append($"Name = \"{methodSymbol.Name}\",");
        sourceCodeWriter.Append($"GenericTypeCount = {methodSymbol.TypeParameters.Length},");
        var safeReturnTypeName = CodeGenerationHelpers.ContainsTypeParameter(methodSymbol.ReturnType) ? "object" : methodSymbol.ReturnType.GloballyQualified();
        sourceCodeWriter.Append($"ReturnType = typeof({safeReturnTypeName}),");
        sourceCodeWriter.Append($"ReturnTypeReference = {CodeGenerationHelpers.GenerateTypeReference(methodSymbol.ReturnType)},");

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
                GenerateParameterInformation(sourceCodeWriter, compilation, parameter, ArgumentsType.ClassConstructor,
                null);
            }

            sourceCodeWriter.Append("],");
        }

        sourceCodeWriter.Append("Class = ");
        GenerateClassInformation(sourceCodeWriter, compilation, namedTypeSymbol);

        sourceCodeWriter.Append("}");
        sourceCodeWriter.Append($"{suffix}");
        sourceCodeWriter.AppendLine();
    }

    public static void GenerateMembers(ICodeWriter sourceCodeWriter, Compilation compilation, INamedTypeSymbol namedTypeSymbol, ImmutableArray<IParameterSymbol> parameters, IPropertySymbol? property, ArgumentsType argumentsType)
    {
        if (parameters.Length == 0 && property is null)
        {
            sourceCodeWriter.Append("[],");
            return;
        }

        sourceCodeWriter.Append("[");

        if (property is not null)
        {
            GeneratePropertyInformation(sourceCodeWriter, compilation, property, namedTypeSymbol);
        }

        foreach (var parameter in parameters)
        {
            GenerateParameterInformation(sourceCodeWriter, compilation, parameter, argumentsType, null);
        }

        sourceCodeWriter.Append("],");
    }

    public static void GeneratePropertyInformation(ICodeWriter sourceCodeWriter,
        Compilation compilation, IPropertySymbol property, INamedTypeSymbol namedTypeSymbol)
    {
        sourceCodeWriter.Append("new global::TUnit.Core.PropertyMetadata");
        sourceCodeWriter.Append("{");
        var safeTypeNameForReflection = CodeGenerationHelpers.ContainsTypeParameter(namedTypeSymbol) ? "object" : namedTypeSymbol.GloballyQualified();
        sourceCodeWriter.Append($"ReflectionInfo = typeof({safeTypeNameForReflection}).GetProperty(\"{property.Name}\"),");
        var safePropertyTypeName = CodeGenerationHelpers.ContainsTypeParameter(property.Type) ? "object" : property.Type.GloballyQualified();
        sourceCodeWriter.Append($"Type = typeof({safePropertyTypeName}),");
        sourceCodeWriter.Append($"Name = \"{property.Name}\",");
        sourceCodeWriter.Append($"IsStatic = {property.IsStatic.ToString().ToLower()},");
        sourceCodeWriter.Append($"Getter = {GetPropertyAccessor(namedTypeSymbol, property)},");

        // For now, always set ClassMetadata to null to avoid circular references
        // The ClassMetadata will be available through the cache if needed at runtime
        sourceCodeWriter.Append("ClassMetadata = null,");

        sourceCodeWriter.Append("}");
        sourceCodeWriter.Append(",");
    }


    private static string GetPropertyAccessor(INamedTypeSymbol namedTypeSymbol, IPropertySymbol property)
    {
        var safeTypeName = CodeGenerationHelpers.ContainsTypeParameter(namedTypeSymbol) ? "object" : namedTypeSymbol.GloballyQualified();
        return property.IsStatic
            ? $"_ => {safeTypeName}.{property.Name}"
            : $"o => (({safeTypeName})o).{property.Name}";
    }

    public static void GenerateParameterInformation(ICodeWriter sourceCodeWriter,
        Compilation context,
        IParameterSymbol parameter, ArgumentsType argumentsType,
        IDictionary<string, string>? genericSubstitutions)
    {
        var type = parameter.Type.GloballyQualified();

        if (parameter.Type.IsGenericDefinition() || CodeGenerationHelpers.ContainsTypeParameter(parameter.Type))
        {
            type = GetTypeOrSubstitution(parameter.Type);
        }

        sourceCodeWriter.Append($"new global::TUnit.Core.ParameterMetadata<{type}>");
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append($"Name = \"{parameter.Name}\",");
        sourceCodeWriter.Append($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(parameter.Type)},");

        sourceCodeWriter.Append("ReflectionInfo = null!,");

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
