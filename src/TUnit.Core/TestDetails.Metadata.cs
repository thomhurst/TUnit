using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test metadata - implements <see cref="ITestDetailsMetadata"/> interface
/// </summary>
public partial class TestDetails
{
    // Explicit interface implementation for ITestDetailsMetadata
    List<string> ITestDetailsMetadata.Categories => Categories;
    Dictionary<string, List<string>> ITestDetailsMetadata.CustomProperties => CustomProperties;
    IReadOnlyDictionary<Type, IReadOnlyList<Attribute>> ITestDetailsMetadata.AttributesByType => AttributesByType;
    bool ITestDetailsMetadata.HasAttribute<T>() => HasAttribute<T>();
    IEnumerable<T> ITestDetailsMetadata.GetAttributes<T>() => GetAttributes<T>();
    IReadOnlyList<Attribute> ITestDetailsMetadata.GetAllAttributes() => GetAllAttributes();

}
