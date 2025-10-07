using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is equal to an expected value.
/// Generic implementation that works for all types.
/// </summary>
public class EqualsAssertion<TValue> : Assertion<TValue>
{
    private readonly TValue _expected;
    private readonly IEqualityComparer<TValue>? _comparer;
    private object? _tolerance;
    private readonly HashSet<Type> _ignoredTypes = new();

    /// <summary>
    /// Gets the expected value for this equality assertion.
    /// Used by extension methods like Within() to create derived assertions.
    /// </summary>
    public TValue Expected => _expected;

    /// <summary>
    /// Gets the evaluation context for this assertion.
    /// Used by extension methods to create derived assertions.
    /// </summary>
    public new EvaluationContext<TValue> Context => base.Context;

    /// <summary>
    /// Gets the expression builder for this assertion.
    /// Used by extension methods to continue building assertion expressions.
    /// </summary>
    public new StringBuilder ExpressionBuilder => base.ExpressionBuilder;

    public EqualsAssertion(
        EvaluationContext<TValue> context,
        TValue expected,
        StringBuilder expressionBuilder,
        IEqualityComparer<TValue>? comparer = null)
        : base(context, expressionBuilder)
    {
        _expected = expected;
        _comparer = comparer;
    }

    /// <summary>
    /// Sets a tolerance for numeric/temporal comparisons.
    /// Returns this assertion for fluent chaining.
    /// </summary>
    public EqualsAssertion<TValue> WithTolerance(object tolerance)
    {
        _tolerance = tolerance;
        return this;
    }

    /// <summary>
    /// Ignores properties/fields of the specified type during equivalence comparison.
    /// Performs deep object comparison while skipping members of the ignored type.
    /// </summary>
    public EqualsAssertion<TValue> IgnoringType<TIgnore>()
    {
        _ignoredTypes.Add(typeof(TIgnore));
        ExpressionBuilder.Append($".IgnoringType<{typeof(TIgnore).Name}>()");
        return this;
    }

    /// <summary>
    /// Ignores properties/fields of the specified type during equivalence comparison.
    /// Non-generic version for runtime type specification.
    /// </summary>
    public EqualsAssertion<TValue> IgnoringType(Type type)
    {
        _ignoredTypes.Add(type);
        ExpressionBuilder.Append($".IgnoringType(typeof({type.Name}))");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        // Handle tolerance-based comparisons for specific types
        if (_tolerance != null && value != null)
        {
            // TimeSpan
            if (typeof(TValue) == typeof(TimeSpan) && _tolerance is TimeSpan timeSpanTolerance)
            {
                var actual = (TimeSpan)(object)value;
                var expected = (TimeSpan)(object)_expected!;
                var difference = actual > expected ? actual - expected : expected - actual;
                if (difference <= timeSpanTolerance)
                    return Task.FromResult(AssertionResult.Passed);
                return Task.FromResult(AssertionResult.Failed($"found {value}, difference {difference} exceeds tolerance {timeSpanTolerance}"));
            }

            // DateTime
            if (typeof(TValue) == typeof(DateTime) && _tolerance is TimeSpan dateTimeTolerance)
            {
                var actual = (DateTime)(object)value;
                var expected = (DateTime)(object)_expected!;
                var difference = actual > expected ? actual - expected : expected - actual;
                if (difference <= dateTimeTolerance)
                    return Task.FromResult(AssertionResult.Passed);
                return Task.FromResult(AssertionResult.Failed($"found {value}, difference {difference} exceeds tolerance {dateTimeTolerance}"));
            }

            // DateTimeOffset
            if (typeof(TValue) == typeof(DateTimeOffset) && _tolerance is TimeSpan dateTimeOffsetTolerance)
            {
                var actual = (DateTimeOffset)(object)value;
                var expected = (DateTimeOffset)(object)_expected!;
                var difference = actual > expected ? actual - expected : expected - actual;
                if (difference <= dateTimeOffsetTolerance)
                    return Task.FromResult(AssertionResult.Passed);
                return Task.FromResult(AssertionResult.Failed($"found {value}, difference {difference} exceeds tolerance {dateTimeOffsetTolerance}"));
            }

#if NET6_0_OR_GREATER
            // TimeOnly
            if (typeof(TValue) == typeof(TimeOnly) && _tolerance is TimeSpan timeOnlyTolerance)
            {
                var actual = (TimeOnly)(object)value;
                var expected = (TimeOnly)(object)_expected!;
                var difference = actual > expected ? actual.ToTimeSpan() - expected.ToTimeSpan() : expected.ToTimeSpan() - actual.ToTimeSpan();
                if (difference <= timeOnlyTolerance)
                    return Task.FromResult(AssertionResult.Passed);
                return Task.FromResult(AssertionResult.Failed($"found {value}, difference {difference} exceeds tolerance {timeOnlyTolerance}"));
            }
#endif

            // int
            if (typeof(TValue) == typeof(int) && _tolerance is int intTolerance)
            {
                var actual = (int)(object)value;
                var expected = (int)(object)_expected!;
                var difference = actual > expected ? actual - expected : expected - actual;
                if (difference <= intTolerance)
                    return Task.FromResult(AssertionResult.Passed);
                return Task.FromResult(AssertionResult.Failed($"found {value}, difference {difference} exceeds tolerance {intTolerance}"));
            }

            // long
            if (typeof(TValue) == typeof(long) && _tolerance is long longTolerance)
            {
                var actual = (long)(object)value;
                var expected = (long)(object)_expected!;
                var difference = actual > expected ? actual - expected : expected - actual;
                if (difference <= longTolerance)
                    return Task.FromResult(AssertionResult.Passed);
                return Task.FromResult(AssertionResult.Failed($"found {value}, difference {difference} exceeds tolerance {longTolerance}"));
            }

            // double
            if (typeof(TValue) == typeof(double) && _tolerance is double doubleTolerance)
            {
                var actual = (double)(object)value;
                var expected = (double)(object)_expected!;
                var difference = actual > expected ? actual - expected : expected - actual;
                if (difference <= doubleTolerance)
                    return Task.FromResult(AssertionResult.Passed);
                return Task.FromResult(AssertionResult.Failed($"found {value}, difference {difference} exceeds tolerance {doubleTolerance}"));
            }

            // decimal
            if (typeof(TValue) == typeof(decimal) && _tolerance is decimal decimalTolerance)
            {
                var actual = (decimal)(object)value;
                var expected = (decimal)(object)_expected!;
                var difference = actual > expected ? actual - expected : expected - actual;
                if (difference <= decimalTolerance)
                    return Task.FromResult(AssertionResult.Passed);
                return Task.FromResult(AssertionResult.Failed($"found {value}, difference {difference} exceeds tolerance {decimalTolerance}"));
            }
        }

        // Deep comparison with ignored types
        if (_ignoredTypes.Count > 0)
        {
            var result = DeepEquals(value, _expected, _ignoredTypes);
            if (result.IsSuccess)
                return Task.FromResult(AssertionResult.Passed);
            return Task.FromResult(AssertionResult.Failed(result.Message ?? $"found {value}"));
        }

        // Standard equality comparison
        var comparer = _comparer ?? EqualityComparer<TValue>.Default;

        if (comparer.Equals(value!, _expected))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Deep comparison requires reflection access to all public properties and fields of runtime types")]
    private static (bool IsSuccess, string? Message) DeepEquals(object? actual, object? expected, HashSet<Type> ignoredTypes)
    {
        // Handle nulls
        if (actual == null && expected == null)
            return (true, null);
        if (actual == null || expected == null)
            return (false, $"one value is null: actual={actual}, expected={expected}");

        var type = actual.GetType();
        if (type != expected.GetType())
            return (false, $"types differ: {actual.GetType().Name} vs {expected.GetType().Name}");

        // Get all public properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            // Skip if property type should be ignored
            var propType = prop.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;
            if (ignoredTypes.Contains(underlyingType))
                continue;

            var actualValue = prop.GetValue(actual);
            var expectedValue = prop.GetValue(expected);

            if (!Equals(actualValue, expectedValue))
                return (false, $"property {prop.Name} differs: {actualValue} vs {expectedValue}");
        }

        // Get all public fields
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            // Skip if field type should be ignored
            var fieldType = field.FieldType;
            var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
            if (ignoredTypes.Contains(underlyingType))
                continue;

            var actualValue = field.GetValue(actual);
            var expectedValue = field.GetValue(expected);

            if (!Equals(actualValue, expectedValue))
                return (false, $"field {field.Name} differs: {actualValue} vs {expectedValue}");
        }

        return (true, null);
    }

    protected override string GetExpectation() =>
        _tolerance != null
            ? $"to equal {_expected} within {_tolerance}"
            : $"to be equal to {_expected}";
}
