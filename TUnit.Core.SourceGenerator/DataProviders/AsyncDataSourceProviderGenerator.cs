using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.DataProviders;

/// <summary>
/// Generates data providers for attributes inheriting from AsyncDataSourceGeneratorAttribute
/// </summary>
public class AsyncDataSourceProviderGenerator : IDataProviderGenerator
{
    public bool CanGenerate(AttributeData attribute)
    {
        return IsAsyncDataSourceGenerator(attribute);
    }

    public string GenerateProvider(AttributeData attribute, TestMetadataGenerationContext context, DataProviderType providerType)
    {
        using var writer = new CodeWriter("", includeHeader: false);

        if (providerType == DataProviderType.ClassParameters)
        {
            GenerateAsyncDataSourceProviderForClass(writer, attribute, context);
        }
        else
        {
            GenerateAsyncDataSourceProviderForMethod(writer, attribute, context);
        }

        return writer.ToString().Trim();
    }

    private static bool IsAsyncDataSourceGenerator(AttributeData attribute)
    {
        var baseType = attribute.AttributeClass?.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "AsyncDataSourceGeneratorAttribute")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static void GenerateAsyncDataSourceProviderForClass(CodeWriter writer, AttributeData attribute, TestMetadataGenerationContext context)
    {
        // Create async generator with compile-time metadata for class constructor parameters
        var attrType = attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

        writer.Append($"new TUnit.Core.AsyncDataGeneratorProvider(new {attrType}(), ");
        writer.Append("new TUnit.Core.CompileTimeDataGeneratorMetadata { ");

        // Generate MembersToGenerate array from constructor parameters
        writer.Append("MembersToGenerate = new TUnit.Core.MemberMetadata[] { ");

        // Find the constructor
        var constructors = context.TestInfo.TypeSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(c => c.Parameters.Length)
            .ToList();

        var constructor = constructors.FirstOrDefault(c => c.Parameters.Length > 0) ?? constructors.FirstOrDefault();

        if (constructor != null)
        {
            var parameters = constructor.Parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    writer.Append(", ");
                }
                var param = parameters[i];
                var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                writer.Append($"new TUnit.Core.ParameterMetadata(typeof({paramType})) {{ ");
                writer.Append($"Name = \"{param.Name}\", ");
                writer.Append($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(param.Type)}, ");
                writer.Append($"ReflectionInfo = typeof({context.TestInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}).GetConstructor({GenerateParameterTypesArray(constructor.Parameters)})!.GetParameters()[{i}], ");
                writer.Append("}");
            }
        }

        writer.Append(" }, ");

        // Set other required properties
        writer.Append($"TestInformation = {GenerateMethodMetadataUsingWriter(context.TestInfo)}, ");
        writer.Append("Type = TUnit.Core.Enums.DataGeneratorType.ClassParameters ");
        writer.Append("})");
    }

    private static void GenerateAsyncDataSourceProviderForMethod(CodeWriter writer, AttributeData attribute, TestMetadataGenerationContext context)
    {
        // Create async generator with compile-time metadata
        var attrType = attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

        writer.Append($"new TUnit.Core.AsyncDataGeneratorProvider(new {attrType}(), ");
        writer.Append("new TUnit.Core.CompileTimeDataGeneratorMetadata { ");

        // Generate MembersToGenerate array from method parameters
        writer.Append("MembersToGenerate = new TUnit.Core.MemberMetadata[] { ");

        var parameters = context.TestInfo.MethodSymbol.Parameters;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
            {
                writer.Append(", ");
            }
            var param = parameters[i];
            var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
            writer.Append($"new TUnit.Core.ParameterMetadata(typeof({paramType})) {{ ");
            writer.Append($"Name = \"{param.Name}\", ");
            writer.Append($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(param.Type)}, ");
            writer.Append($"ReflectionInfo = typeof({context.TestInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}).GetMethod(\"{context.TestInfo.MethodSymbol.Name}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, {GenerateParameterTypesArray(context.TestInfo.MethodSymbol)}, null)!.GetParameters()[{i}], ");
            writer.Append("}");
        }

        writer.Append(" }, ");

        // Set other required properties
        writer.Append($"TestInformation = {GenerateMethodMetadataUsingWriter(context.TestInfo)}, ");
        writer.Append("Type = TUnit.Core.Enums.DataGeneratorType.TestParameters ");
        writer.Append("})");
    }

    private static string GenerateParameterTypesArray(IEnumerable<IParameterSymbol> parameters)
    {
        var parameterList = parameters.ToList();

        if (!parameterList.Any())
        {
            return "System.Type.EmptyTypes";
        }

        // Check if any parameter contains type parameters
        if (parameterList.Any(p => ContainsTypeParameter(p.Type)))
        {
            // Return null to indicate that parameter type matching should be done at runtime
            return "null";
        }

        var parameterTypes = parameterList
            .Select(p => $"typeof({p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))})")
            .ToArray();

        return $"new System.Type[] {{ {string.Join(", ", parameterTypes)} }}";
    }

    private static string GenerateParameterTypesArray(IMethodSymbol method)
    {
        return GenerateParameterTypesArray(method.Parameters);
    }

    private static bool ContainsTypeParameter(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol)
        {
            return true;
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return ContainsTypeParameter(arrayType.ElementType);
        }

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            return namedType.TypeArguments.Any(ContainsTypeParameter);
        }

        return false;
    }

    private static string GenerateMethodMetadataUsingWriter(TestMethodMetadata testInfo)
    {
        using var writer = new CodeWriter("", includeHeader: false);

        // Use the existing writer to generate the metadata
        SourceInformationWriter.GenerateMethodInformation(writer, testInfo.Context.SemanticModel.Compilation, testInfo.TypeSymbol, testInfo.MethodSymbol, null, ',');

        // Remove the trailing comma and newline
        var result = writer.ToString().TrimEnd('\r', '\n', ',');
        return result;
    }
}
