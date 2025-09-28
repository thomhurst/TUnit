using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
///  generic equality assertion with lazy evaluation
/// </summary>
public class GenericEqualToAssertion<TActual> : AssertionBase<TActual>
{
    private readonly TActual _expected;
    private IEqualityComparer<TActual>? _comparer;
    private Func<TActual?, TActual?, bool>? _customComparison;
#if NET
    private object? _tolerance;
#endif

    // Internal property to access expected value for extensions
    internal TActual ExpectedValue => _expected;

    // Constructor takes lazy value provider and expected value
    public GenericEqualToAssertion(Func<Task<TActual>> actualValueProvider, TActual expected)
        : base(actualValueProvider)
    {
        _expected = expected;
    }

    public GenericEqualToAssertion(Func<TActual> actualValueProvider, TActual expected)
        : base(actualValueProvider)
    {
        _expected = expected;
    }

    public GenericEqualToAssertion(TActual actualValue, TActual expected)
        : base(actualValue)
    {
        _expected = expected;
    }

    // Fluent configuration methods - NO EVALUATION happens here
    public GenericEqualToAssertion<TActual> WithComparer(IEqualityComparer<TActual> comparer)
    {
        _comparer = comparer;
        return this;
    }

    public GenericEqualToAssertion<TActual> WithComparison(Func<TActual?, TActual?, bool> comparison)
    {
        _customComparison = comparison;
        return this;
    }

#if NET
    public GenericEqualToAssertion<TActual> Within<T>(T tolerance) where T : IComparable<T>
    {
        if (typeof(TActual) == typeof(T) ||
            typeof(TActual) == typeof(double) ||
            typeof(TActual) == typeof(float) ||
            typeof(TActual) == typeof(decimal))
        {
            _tolerance = tolerance;
        }
        return this;
    }
#endif

    /// <summary>
    /// The actual assertion logic - ONLY called when awaited
    /// </summary>
    protected override async Task<AssertionResult> AssertAsync()
    {
        // NOW we get the actual value (lazy evaluation)
        var actual = await GetActualValueAsync();

        // Check for custom comparison first
        if (_customComparison != null)
        {
            if (_customComparison(actual, _expected))
            {
                return AssertionResult.Passed;
            }
            return AssertionResult.Fail(GenerateFailureMessage(actual, _expected));
        }

#if NET
        // Check for tolerance-based comparison
        if (_tolerance != null && IsNumericType())
        {
            if (CompareWithTolerance(actual, _expected, _tolerance))
            {
                return AssertionResult.Passed;
            }
            return AssertionResult.Fail($"Expected {_expected} ±{_tolerance} but was {actual}");
        }
#endif

        // Use custom comparer if provided
        if (_comparer != null)
        {
            if (_comparer.Equals(actual, _expected))
            {
                return AssertionResult.Passed;
            }
            return AssertionResult.Fail(GenerateFailureMessage(actual, _expected));
        }

        // Default equality
        if (EqualityComparer<TActual>.Default.Equals(actual, _expected))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Fail(GenerateFailureMessage(actual, _expected));
    }

    private string GenerateFailureMessage(TActual? actual, TActual? expected)
    {
        if (actual == null && expected != null)
        {
            return $"Expected {expected} but was null";
        }

        if (actual != null && expected == null)
        {
            return $"Expected null but was {actual}";
        }

        // Check for collection differences
        if (actual is System.Collections.IEnumerable actualEnum &&
            expected is System.Collections.IEnumerable expectedEnum)
        {
            return $"Expected {FormatValue(expected)} but was {FormatValue(actual)}\nCollections differ";
        }

        return $"Expected {FormatValue(expected)} but was {FormatValue(actual)}";
    }

    private string FormatValue(TActual? value)
    {
        if (value == null) return "null";
        if (value is string str) return $"\"{str}\"";
        return value.ToString() ?? "null";
    }

#if NET
    private bool IsNumericType()
    {
        var type = typeof(TActual);
        return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
               type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte) ||
               type == typeof(double) || type == typeof(float) || type == typeof(decimal);
    }

    private bool CompareWithTolerance(TActual? actual, TActual? expected, object tolerance)
    {
        if (actual == null || expected == null) return false;

        try
        {
            dynamic actualDyn = actual;
            dynamic expectedDyn = expected;
            dynamic toleranceDyn = tolerance;

            return actualDyn >= expectedDyn - toleranceDyn && actualDyn <= expectedDyn + toleranceDyn;
        }
        catch
        {
            // Fallback to regular comparison if dynamic fails
            return EqualityComparer<TActual>.Default.Equals(actual, expected);
        }
    }
#endif
}
