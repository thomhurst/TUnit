namespace TUnit.Core;

/// <summary>
/// Lightweight test descriptor for fast enumeration and filtering.
/// Contains only the minimal information needed to identify and filter tests
/// without materializing full test metadata.
/// </summary>
/// <remarks>
/// <para>
/// This struct is designed to minimize allocations during test discovery.
/// The full <see cref="TestMetadata"/> is only created when <see cref="Materializer"/>
/// is invoked, which happens only for tests that pass filtering.
/// </para>
/// <para>
/// Filter hints (Categories, Properties) are pre-computed at compile time
/// by the source generator, avoiding runtime attribute instantiation.
/// </para>
/// </remarks>
public readonly struct TestDescriptor
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// Format: "{Namespace}.{ClassName}.{MethodName}:{DataIndex}"
    /// </summary>
    public required string TestId { get; init; }

    /// <summary>
    /// Gets the simple name of the test class.
    /// </summary>
    public required string ClassName { get; init; }

    /// <summary>
    /// Gets the name of the test method.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Gets the fully qualified name of the test.
    /// Format: "{Namespace}.{ClassName}.{MethodName}"
    /// </summary>
    public required string FullyQualifiedName { get; init; }

    /// <summary>
    /// Gets the source file path where the test is defined.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the line number where the test is defined.
    /// </summary>
    public required int LineNumber { get; init; }

    /// <summary>
    /// Gets the pre-extracted category values from CategoryAttribute.
    /// Pre-computed at compile time to avoid runtime attribute instantiation.
    /// </summary>
    public string[] Categories { get; init; }

    /// <summary>
    /// Gets the pre-extracted property values from PropertyAttribute.
    /// Format: "key=value" pairs.
    /// Pre-computed at compile time to avoid runtime attribute instantiation.
    /// </summary>
    public string[] Properties { get; init; }

    /// <summary>
    /// Gets whether this test has data sources (is parameterized).
    /// When true, materialization may yield multiple TestMetadata instances.
    /// </summary>
    public bool HasDataSource { get; init; }

    /// <summary>
    /// Gets the repeat count from RepeatAttribute, or 0 if not present.
    /// Pre-extracted to avoid attribute instantiation.
    /// </summary>
    public int RepeatCount { get; init; }

    /// <summary>
    /// Gets the dependencies for this test (from DependsOnAttribute).
    /// Pre-extracted at compile time to enable dependency expansion during filtering.
    /// Format: "ClassName:MethodName" for cross-class, ":MethodName" for same-class,
    /// "ClassName:" for all tests in class.
    /// </summary>
    public string[] DependsOn { get; init; }

    /// <summary>
    /// Gets the factory delegate that creates the full TestMetadata.
    /// Only invoked for tests that pass filtering.
    /// </summary>
    /// <remarks>
    /// The delegate accepts a test session ID and cancellation token, returning an async enumerable
    /// of TestMetadata. For non-parameterized tests, this yields a single item.
    /// For parameterized tests, this yields one item per data row.
    /// </remarks>
    public required Func<string, CancellationToken, IAsyncEnumerable<TestMetadata>> Materializer { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDescriptor"/> struct.
    /// </summary>
    public TestDescriptor()
    {
        Categories = [];
        Properties = [];
        DependsOn = [];
    }

    /// <summary>
    /// Checks if this test matches the specified category.
    /// </summary>
    /// <param name="category">The category to match.</param>
    /// <returns>True if the test has the specified category.</returns>
    public bool HasCategory(string category)
    {
        var categories = Categories;
        for (var i = 0; i < categories.Length; i++)
        {
            if (string.Equals(categories[i], category, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if this test has the specified property.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value (optional, if null only key is checked).</param>
    /// <returns>True if the test has the specified property.</returns>
    public bool HasProperty(string key, string? value = null)
    {
        var prefix = value == null ? $"{key}=" : $"{key}={value}";
        var properties = Properties;
        for (var i = 0; i < properties.Length; i++)
        {
            if (value == null)
            {
                if (properties[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else
            {
                if (string.Equals(properties[i], prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
