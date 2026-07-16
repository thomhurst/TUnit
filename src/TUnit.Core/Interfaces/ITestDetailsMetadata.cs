namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test metadata including categories, custom properties, and attributes.
/// Accessed via <see cref="TestDetails.Attributes"/>.
/// </summary>
public interface ITestDetailsMetadata
{
    /// <summary>
    /// Gets the collection of categories assigned to this test.
    /// </summary>
    List<string> Categories { get; }

    /// <summary>
    /// Gets the custom properties associated with this test.
    /// </summary>
    Dictionary<string, List<string>> CustomProperties { get; }

    /// <summary>
    /// Gets all attributes grouped by their type.
    /// </summary>
    IReadOnlyDictionary<Type, IReadOnlyList<Attribute>> AttributesByType { get; }

    /// <summary>
    /// Checks if the test has an attribute of the specified type.
    /// </summary>
    /// <typeparam name="T">The attribute type to check for.</typeparam>
    /// <returns>True if the test has at least one attribute of the specified type; otherwise, false.</returns>
    bool HasAttribute<T>() where T : Attribute;

    /// <summary>
    /// Gets all attributes of the specified type.
    /// </summary>
    /// <typeparam name="T">The attribute type to retrieve.</typeparam>
    /// <returns>An enumerable of attributes of the specified type.</returns>
    IEnumerable<T> GetAttributes<T>() where T : Attribute;

    /// <summary>
    /// Gets all attributes as a flattened collection.
    /// Cached after first access for performance.
    /// </summary>
    /// <returns>All attributes associated with this test.</returns>
    IReadOnlyList<Attribute> GetAllAttributes();
}
