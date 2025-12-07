using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Conditions.Helpers;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is equal to an expected value.
/// Generic implementation that works for all types.
/// </summary>
[AssertionExtension("IsEqualTo")]
public class EqualsAssertion<TValue> : Assertion<TValue>
{
    private readonly TValue? _expected;
    private readonly IEqualityComparer<TValue>? _comparer;
    private readonly HashSet<Type> _ignoredTypes = new();

    // Cache reflection results for better performance in deep comparison
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldCache = new();

    /// <summary>
    /// Gets the expected value for this equality assertion.
    /// </summary>
    public TValue? Expected => _expected;

    // Constructor 1: Just expected value
    public EqualsAssertion(
        AssertionContext<TValue> context,
        TValue? expected)
        : base(context)
    {
        _expected = expected;
        _comparer = null;
    }

    // Constructor 2: Expected value with comparer
    public EqualsAssertion(
        AssertionContext<TValue> context,
        TValue? expected,
        IEqualityComparer<TValue> comparer)
        : base(context)
    {
        _expected = expected;
        _comparer = comparer;
    }

    /// <summary>
    /// Ignores properties/fields of the specified type during equivalence comparison.
    /// Performs deep object comparison while skipping members of the ignored type.
    /// </summary>
    public EqualsAssertion<TValue> IgnoringType<TIgnore>()
    {
        _ignoredTypes.Add(typeof(TIgnore));
        Context.ExpressionBuilder.Append($".IgnoringType<{typeof(TIgnore).Name}>()");
        return this;
    }

    /// <summary>
    /// Ignores properties/fields of the specified type during equivalence comparison.
    /// Non-generic version for runtime type specification.
    /// </summary>
    public EqualsAssertion<TValue> IgnoringType(Type type)
    {
        _ignoredTypes.Add(type);
        Context.ExpressionBuilder.Append($".IgnoringType(typeof({type.Name}))");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().FullName}"));
        }

        // Deep comparison with ignored types
        if (_ignoredTypes.Count > 0)
        {
            // Use reference-based tracking to detect cycles
            var visited = new HashSet<object>(ReferenceEqualityComparer<object>.Instance);
            var result = DeepEquals(value, _expected, _ignoredTypes, visited);
            if (result.IsSuccess)
            {
                return Task.FromResult(AssertionResult.Passed);
            }

            return Task.FromResult(AssertionResult.Failed(result.Message ?? $"found {value}"));
        }

        // Standard equality comparison
        var comparer = _comparer ?? EqualityComparer<TValue>.Default;

        if (comparer.Equals(value!, _expected!))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Deep comparison requires reflection access to all public properties and fields of runtime types")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Deep comparison requires reflection access to all public properties and fields of runtime types")]
    private static (bool IsSuccess, string? Message) DeepEquals(object? actual, object? expected, HashSet<Type> ignoredTypes, HashSet<object> visited)
    {
        // Handle nulls
        if (actual == null && expected == null)
        {
            return (true, null);
        }

        if (actual == null || expected == null)
        {
            return (false, $"one value is null: actual={actual}, expected={expected}");
        }

        var type = actual.GetType();
        if (type != expected.GetType())
        {
            return (false, $"types differ: {actual.GetType().Name} vs {expected.GetType().Name}");
        }

        // Cycle detection - if we've already visited this object, assume equal to avoid infinite recursion
        if (!visited.Add(actual))
        {
            return (true, null);
        }

        // For value types and strings, use standard equality
        if (type.IsValueType || type == typeof(string))
        {
            if (!Equals(actual, expected))
            {
                return (false, $"values differ: {actual} vs {expected}");
            }

            return (true, null);
        }

        // Get all public properties (cached for performance)
        var properties = PropertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        foreach (var prop in properties)
        {
            // Skip if property type should be ignored
            var propType = prop.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;
            if (ignoredTypes.Contains(underlyingType))
            {
                continue;
            }

            var actualValue = prop.GetValue(actual);
            var expectedValue = prop.GetValue(expected);

            // Recursively compare complex objects
            if (actualValue != null && !actualValue.GetType().IsValueType && actualValue.GetType() != typeof(string))
            {
                var nestedResult = DeepEquals(actualValue, expectedValue, ignoredTypes, visited);
                if (!nestedResult.IsSuccess)
                {
                    return (false, $"property {prop.Name} differs: {nestedResult.Message}");
                }
            }
            else
            {
                if (!Equals(actualValue, expectedValue))
                {
                    return (false, $"property {prop.Name} differs: {actualValue} vs {expectedValue}");
                }
            }
        }

        // Get all public fields (cached for performance)
        var fields = FieldCache.GetOrAdd(type, t => t.GetFields(BindingFlags.Public | BindingFlags.Instance));
        foreach (var field in fields)
        {
            // Skip if field type should be ignored
            var fieldType = field.FieldType;
            var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
            if (ignoredTypes.Contains(underlyingType))
            {
                continue;
            }

            var actualValue = field.GetValue(actual);
            var expectedValue = field.GetValue(expected);

            // Recursively compare complex objects
            if (actualValue != null && !actualValue.GetType().IsValueType && actualValue.GetType() != typeof(string))
            {
                var nestedResult = DeepEquals(actualValue, expectedValue, ignoredTypes, visited);
                if (!nestedResult.IsSuccess)
                {
                    return (false, $"field {field.Name} differs: {nestedResult.Message}");
                }
            }
            else
            {
                if (!Equals(actualValue, expectedValue))
                {
                    return (false, $"field {field.Name} differs: {actualValue} vs {expectedValue}");
                }
            }
        }

        return (true, null);
    }

    protected override string GetExpectation() => $"to be equal to {(_expected is string s ? $"\"{s}\"" : _expected)}";
}
