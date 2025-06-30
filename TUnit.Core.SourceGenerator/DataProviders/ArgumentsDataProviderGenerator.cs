using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.DataProviders;

/// <summary>
/// Generates data providers for ArgumentsAttribute
/// </summary>
internal class ArgumentsDataProviderGenerator : IDataProviderGenerator
{
    public bool CanGenerate(AttributeData attribute)
    {
        return attribute.AttributeClass?.Name == "ArgumentsAttribute";
    }

    public string GenerateProvider(AttributeData attribute, TestMetadataGenerationContext context, DataProviderType providerType)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        writer.Append("new TUnit.Core.ArgumentsDataProvider(");
        GenerateArgumentsForAttribute(writer, attribute);
        writer.Append(")");
        return writer.ToString().Trim();
    }

    private static void GenerateArgumentsForAttribute(CodeWriter writer, AttributeData attribute)
    {
        var args = attribute.ConstructorArguments;
        // ArgumentsAttribute constructor takes params object?[]
        if (args.Length == 1 && args[0].Kind == TypedConstantKind.Array)
        {
            // Handle params array case
            var values = args[0].Values;
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) writer.Append(", ");
                writer.Append(TypedConstantParser.GetRawTypedConstantValue(values[i]));
            }
        }
        else
        {
            // Handle individual arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) writer.Append(", ");
                writer.Append(TypedConstantParser.GetRawTypedConstantValue(args[i]));
            }
        }
    }
}
