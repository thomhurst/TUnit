using System.Collections.Concurrent;
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

    // Delegate for tolerance comparison strategies
    private delegate bool ToleranceComparer<T>(T actual, T expected, object tolerance, out string? errorMessage);

    // Static dictionary mapping types to their tolerance comparison strategies
    private static readonly Dictionary<Type, Delegate> ToleranceComparers = new()
    {
        [typeof(TimeSpan)] = new ToleranceComparer<TimeSpan>(CompareTimeSpan),
        [typeof(DateTime)] = new ToleranceComparer<DateTime>(CompareDateTime),
        [typeof(DateTimeOffset)] = new ToleranceComparer<DateTimeOffset>(CompareDateTimeOffset),
#if NET6_0_OR_GREATER
        [typeof(TimeOnly)] = new ToleranceComparer<TimeOnly>(CompareTimeOnly),
#endif
        [typeof(int)] = new ToleranceComparer<int>(CompareInt),
        [typeof(long)] = new ToleranceComparer<long>(CompareLong),
        [typeof(double)] = new ToleranceComparer<double>(CompareDouble),
        [typeof(decimal)] = new ToleranceComparer<decimal>(CompareDecimal)
    };

    // Cache reflection results for better performance in deep comparison
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldCache = new();

    // Tolerance comparison strategies
    private static bool CompareTimeSpan(TimeSpan actual, TimeSpan expected, object tolerance, out string? errorMessage)
    {
        if (tolerance is not TimeSpan timeSpanTolerance)
        {
            errorMessage = null;
            return false;
        }

        var difference = actual > expected ? actual - expected : expected - actual;
        if (difference <= timeSpanTolerance)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"found {actual}, difference {difference} exceeds tolerance {timeSpanTolerance}";
        return false;
    }

    private static bool CompareDateTime(DateTime actual, DateTime expected, object tolerance, out string? errorMessage)
    {
        if (tolerance is not TimeSpan dateTimeTolerance)
        {
            errorMessage = null;
            return false;
        }

        var difference = actual > expected ? actual - expected : expected - actual;
        if (difference <= dateTimeTolerance)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"found {actual}, difference {difference} exceeds tolerance {dateTimeTolerance}";
        return false;
    }

    private static bool CompareDateTimeOffset(DateTimeOffset actual, DateTimeOffset expected, object tolerance, out string? errorMessage)
    {
        if (tolerance is not TimeSpan dateTimeOffsetTolerance)
        {
            errorMessage = null;
            return false;
        }

        var difference = actual > expected ? actual - expected : expected - actual;
        if (difference <= dateTimeOffsetTolerance)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"found {actual}, difference {difference} exceeds tolerance {dateTimeOffsetTolerance}";
        return false;
    }

#if NET6_0_OR_GREATER
    private static bool CompareTimeOnly(TimeOnly actual, TimeOnly expected, object tolerance, out string? errorMessage)
    {
        if (tolerance is not TimeSpan timeOnlyTolerance)
        {
            errorMessage = null;
            return false;
        }

        var difference = actual > expected ? actual.ToTimeSpan() - expected.ToTimeSpan() : expected.ToTimeSpan() - actual.ToTimeSpan();
        if (difference <= timeOnlyTolerance)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"found {actual}, difference {difference} exceeds tolerance {timeOnlyTolerance}";
        return false;
    }
#endif

    private static bool CompareInt(int actual, int expected, object tolerance, out string? errorMessage)
    {
        if (tolerance is not int intTolerance)
        {
            errorMessage = null;
            return false;
        }

        var difference = actual > expected ? actual - expected : expected - actual;
        if (difference <= intTolerance)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"found {actual}, difference {difference} exceeds tolerance {intTolerance}";
        return false;
    }

    private static bool CompareLong(long actual, long expected, object tolerance, out string? errorMessage)
    {
        if (tolerance is not long longTolerance)
        {
            errorMessage = null;
            return false;
        }

        var difference = actual > expected ? actual - expected : expected - actual;
        if (difference <= longTolerance)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"found {actual}, difference {difference} exceeds tolerance {longTolerance}";
        return false;
    }

    private static bool CompareDouble(double actual, double expected, object tolerance, out string? errorMessage)
    {
        if (tolerance is not double doubleTolerance)
        {
            errorMessage = null;
            return false;
        }

        var difference = actual > expected ? actual - expected : expected - actual;
        if (difference <= doubleTolerance)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"found {actual}, difference {difference} exceeds tolerance {doubleTolerance}";
        return false;
    }

    private static bool CompareDecimal(decimal actual, decimal expected, object tolerance, out string? errorMessage)
    {
        if (tolerance is not decimal decimalTolerance)
        {
            errorMessage = null;
            return false;
        }

        var difference = actual > expected ? actual - expected : expected - actual;
        if (difference <= decimalTolerance)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"found {actual}, difference {difference} exceeds tolerance {decimalTolerance}";
        return false;
    }

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

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Tolerance comparison requires dynamic invocation of known comparer delegates")]
    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        // Handle tolerance-based comparisons using strategy pattern
        if (_tolerance != null && value != null && ToleranceComparers.TryGetValue(typeof(TValue), out var toleranceComparer))
        {
            // Invoke the appropriate comparer using dynamic invocation
            var compareMethod = toleranceComparer.GetType().GetMethod("Invoke");
            var parameters = new object?[] { value, _expected, _tolerance, null };
            var result = (bool)compareMethod!.Invoke(toleranceComparer, parameters)!;
            var errorMessage = (string?)parameters[3];

            if (result)
                return Task.FromResult(AssertionResult.Passed);
            if (errorMessage != null)
                return Task.FromResult(AssertionResult.Failed(errorMessage));
        }

        // Deep comparison with ignored types
        if (_ignoredTypes.Count > 0)
        {
            // Use reference-based tracking to detect cycles
            var visited = new HashSet<object>(new ReferenceEqualityComparer());
            var result = DeepEquals(value, _expected, _ignoredTypes, visited);
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

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Deep comparison requires reflection access to all public properties and fields of runtime types")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Deep comparison requires reflection access to all public properties and fields of runtime types")]
    private static (bool IsSuccess, string? Message) DeepEquals(object? actual, object? expected, HashSet<Type> ignoredTypes, HashSet<object> visited)
    {
        // Handle nulls
        if (actual == null && expected == null)
            return (true, null);
        if (actual == null || expected == null)
            return (false, $"one value is null: actual={actual}, expected={expected}");

        var type = actual.GetType();
        if (type != expected.GetType())
            return (false, $"types differ: {actual.GetType().Name} vs {expected.GetType().Name}");

        // Cycle detection - if we've already visited this object, assume equal to avoid infinite recursion
        if (!visited.Add(actual))
            return (true, null);

        // For value types and strings, use standard equality
        if (type.IsValueType || type == typeof(string))
        {
            if (!Equals(actual, expected))
                return (false, $"values differ: {actual} vs {expected}");
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
                continue;

            var actualValue = prop.GetValue(actual);
            var expectedValue = prop.GetValue(expected);

            // Recursively compare complex objects
            if (actualValue != null && !actualValue.GetType().IsValueType && actualValue.GetType() != typeof(string))
            {
                var nestedResult = DeepEquals(actualValue, expectedValue, ignoredTypes, visited);
                if (!nestedResult.IsSuccess)
                    return (false, $"property {prop.Name} differs: {nestedResult.Message}");
            }
            else
            {
                if (!Equals(actualValue, expectedValue))
                    return (false, $"property {prop.Name} differs: {actualValue} vs {expectedValue}");
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
                continue;

            var actualValue = field.GetValue(actual);
            var expectedValue = field.GetValue(expected);

            // Recursively compare complex objects
            if (actualValue != null && !actualValue.GetType().IsValueType && actualValue.GetType() != typeof(string))
            {
                var nestedResult = DeepEquals(actualValue, expectedValue, ignoredTypes, visited);
                if (!nestedResult.IsSuccess)
                    return (false, $"field {field.Name} differs: {nestedResult.Message}");
            }
            else
            {
                if (!Equals(actualValue, expectedValue))
                    return (false, $"field {field.Name} differs: {actualValue} vs {expectedValue}");
            }
        }

        return (true, null);
    }

    protected override string GetExpectation() =>
        _tolerance != null
            ? $"to equal {_expected} within {_tolerance}"
            : $"to be equal to {_expected}";

    /// <summary>
    /// Comparer that uses reference equality instead of value equality.
    /// Used for cycle detection in deep comparison.
    /// </summary>
    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
