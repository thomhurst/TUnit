using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is NOT equal to an expected value.
/// </summary>
[AssertionExtension("IsNotEqualTo")]
public class NotEqualsAssertion<TValue> : Assertion<TValue>
{
    private readonly TValue _notExpected;
    private readonly IEqualityComparer<TValue>? _comparer;
    private readonly HashSet<Type> _ignoredTypes = new();

    public NotEqualsAssertion(
        AssertionContext<TValue> context,
        TValue notExpected,
        IEqualityComparer<TValue>? comparer = null)
        : base(context)
    {
        _notExpected = notExpected;
        _comparer = comparer;
    }

    /// <summary>
    /// Ignores properties/fields of the specified type during equivalence comparison.
    /// Performs deep object comparison while skipping members of the ignored type.
    /// </summary>
    public NotEqualsAssertion<TValue> IgnoringType<TIgnore>()
    {
        _ignoredTypes.Add(typeof(TIgnore));
        Context.ExpressionBuilder.Append($".IgnoringType<{typeof(TIgnore).Name}>()");
        return this;
    }

    /// <summary>
    /// Ignores properties/fields of the specified type during equivalence comparison.
    /// Non-generic version for runtime type specification.
    /// </summary>
    public NotEqualsAssertion<TValue> IgnoringType(Type type)
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
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        // Deep comparison with ignored types
        if (_ignoredTypes.Count > 0)
        {
            var result = DeepEquals(value, _notExpected, _ignoredTypes);
            if (!result.IsSuccess)
            {
                return AssertionResult._passedTask;
            }

            return Task.FromResult(AssertionResult.Failed($"both values are equivalent"));
        }

        var comparer = _comparer ?? EqualityComparer<TValue>.Default;

        if (!comparer.Equals(value!, _notExpected))
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"both values are {value}"));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Deep comparison requires reflection access to all public properties and fields of runtime types")]
    private static (bool IsSuccess, string? Message) DeepEquals(object? actual, object? expected, HashSet<Type> ignoredTypes)
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

        // Get all public properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
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

            if (!Equals(actualValue, expectedValue))
            {
                return (false, $"property {prop.Name} differs: {actualValue} vs {expectedValue}");
            }
        }

        // Get all public fields
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
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

            if (!Equals(actualValue, expectedValue))
            {
                return (false, $"field {field.Name} differs: {actualValue} vs {expectedValue}");
            }
        }

        return (true, null);
    }

    protected override string GetExpectation() => $"to not be equal to {_notExpected}";
}
