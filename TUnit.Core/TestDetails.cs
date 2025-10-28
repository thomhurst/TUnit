using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Simplified test details for the new architecture
/// </summary>
public class TestDetails
{
    public required string TestId { get; init; }
    public required string TestName { get; init; }
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public required Type ClassType { get; init; }
    public required string MethodName { get; init; }
    public required object ClassInstance { get; set; }
    public required object?[] TestMethodArguments { get; set; }
    public required object?[] TestClassArguments { get; set; }
    public TimeSpan? Timeout { get; set; }
    public int RetryLimit { get; set; }

    public required MethodMetadata MethodMetadata { get; set; }
    public string TestFilePath { get; set; } = "";
    public int TestLineNumber { get; set; }
    public required Type ReturnType { get; set; }
    public IDictionary<string, object?> TestClassInjectedPropertyArguments { get; init; } = new Dictionary<string, object?>();
    public List<string> Categories { get; } =
    [
    ];
    public Dictionary<string, List<string>> CustomProperties { get; } = new();
    public Type[]? TestClassParameterTypes { get; set; }

    public required IReadOnlyDictionary<Type, IReadOnlyList<Attribute>> AttributesByType { get; init; }

    private IReadOnlyList<Attribute>? _cachedAllAttributes;

    /// <summary>
    /// Checks if the test has an attribute of the specified type.
    /// </summary>
    /// <typeparam name="T">The attribute type to check for.</typeparam>
    /// <returns>True if the test has at least one attribute of the specified type; otherwise, false.</returns>
    public bool HasAttribute<T>() where T : Attribute
        => AttributesByType.ContainsKey(typeof(T));

    /// <summary>
    /// Gets all attributes of the specified type.
    /// </summary>
    /// <typeparam name="T">The attribute type to retrieve.</typeparam>
    /// <returns>An enumerable of attributes of the specified type.</returns>
    public IEnumerable<T> GetAttributes<T>() where T : Attribute
        => AttributesByType.TryGetValue(typeof(T), out var attrs)
            ? attrs.OfType<T>()
            : Enumerable.Empty<T>();

    /// <summary>
    /// Gets all attributes as a flattened collection.
    /// Cached after first access for performance.
    /// </summary>
    /// <returns>All attributes associated with this test.</returns>
    public IReadOnlyList<Attribute> GetAllAttributes()
    {
        if (_cachedAllAttributes == null)
        {
            var allAttrs = new List<Attribute>();
            foreach (var attrList in AttributesByType.Values)
            {
                allAttrs.AddRange(attrList);
            }
            _cachedAllAttributes = allAttrs;
        }
        return _cachedAllAttributes;
    }

    public object?[] ClassMetadataArguments => TestClassArguments;
    
    /// <summary>
    /// Resolved generic type arguments for the test method.
    /// Will be Type.EmptyTypes if the method is not generic.
    /// </summary>
    public Type[] MethodGenericArguments { get; set; } = Type.EmptyTypes;
    
    /// <summary>
    /// Resolved generic type arguments for the test class.
    /// Will be Type.EmptyTypes if the class is not generic.
    /// </summary>
    public Type[] ClassGenericArguments { get; set; } = Type.EmptyTypes;
}

/// <summary>
/// Generic version of TestDetails for compatibility with tests
/// </summary>
public class TestDetails<T> : TestDetails where T : class;
