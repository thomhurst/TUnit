using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.DataProviders;

/// <summary>
/// Default generator that creates an empty data provider
/// </summary>
internal class EmptyDataProviderGenerator : IDataProviderGenerator
{
    public bool CanGenerate(AttributeData attribute)
    {
        // This is the fallback - it can always generate
        return true;
    }

    public string GenerateProvider(AttributeData attribute, TestMetadataGenerationContext context, DataProviderType providerType)
    {
        return "new TUnit.Core.EmptyDataProvider()";
    }
}