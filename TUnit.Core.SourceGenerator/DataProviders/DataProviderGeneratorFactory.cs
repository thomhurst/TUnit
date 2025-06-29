using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.DataProviders;

/// <summary>
/// Factory for creating data provider code based on attributes
/// </summary>
internal static class DataProviderGeneratorFactory
{
    private static readonly IDataProviderGenerator[] Generators =
    [
        new ArgumentsDataProviderGenerator(),
        new MethodDataSourceProviderGenerator(),
        new AsyncDataSourceProviderGenerator(),
        new EmptyDataProviderGenerator() // Fallback - must be last
    ];

    /// <summary>
    /// Generates data provider code for the given attribute
    /// </summary>
    public static string GenerateDataProvider(AttributeData? attribute, TestMetadataGenerationContext context, DataProviderType providerType)
    {
        if (attribute == null)
        {
            return "new TUnit.Core.EmptyDataProvider()";
        }

        foreach (var generator in Generators)
        {
            if (generator.CanGenerate(attribute))
            {
                return generator.GenerateProvider(attribute, context, providerType);
            }
        }

        // Should never reach here due to EmptyDataProviderGenerator fallback
        return "new TUnit.Core.EmptyDataProvider()";
    }
}
