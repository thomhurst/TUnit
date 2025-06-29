using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.DataProviders;

/// <summary>
/// Interface for generating data provider code based on attribute types.
/// Implements the Strategy pattern to handle different data source attributes.
/// </summary>
internal interface IDataProviderGenerator
{
    /// <summary>
    /// Determines if this generator can handle the given attribute
    /// </summary>
    bool CanGenerate(AttributeData attribute);

    /// <summary>
    /// Generates the data provider code for the given attribute
    /// </summary>
    string GenerateProvider(AttributeData attribute, TestMetadataGenerationContext context, DataProviderType providerType);
}

/// <summary>
/// Specifies whether the data provider is for class or method parameters
/// </summary>
internal enum DataProviderType
{
    ClassParameters,
    TestParameters
}
