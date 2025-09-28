using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Range/between assertion with lazy evaluation
/// </summary>
public class BetweenAssertion<TActual> : AssertionBase<TActual>
    where TActual : IComparable<TActual>
{
    private readonly TActual _min;
    private readonly TActual _max;
    private readonly bool _inclusive;
    private readonly bool _shouldBeInRange;

    public BetweenAssertion(Func<Task<TActual>> actualValueProvider, TActual min, TActual max, bool inclusive = true, bool shouldBeInRange = true)
        : base(actualValueProvider)
    {
        _min = min;
        _max = max;
        _inclusive = inclusive;
        _shouldBeInRange = shouldBeInRange;
    }

    public BetweenAssertion(Func<TActual> actualValueProvider, TActual min, TActual max, bool inclusive = true, bool shouldBeInRange = true)
        : base(actualValueProvider)
    {
        _min = min;
        _max = max;
        _inclusive = inclusive;
        _shouldBeInRange = shouldBeInRange;
    }

    public BetweenAssertion(TActual actualValue, TActual min, TActual max, bool inclusive = true, bool shouldBeInRange = true)
        : base(actualValue)
    {
        _min = min;
        _max = max;
        _inclusive = inclusive;
        _shouldBeInRange = shouldBeInRange;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == null)
        {
            return AssertionResult.Fail($"Cannot compare null value to range [{_min}, {_max}]");
        }

        bool isInRange;
        if (_inclusive)
        {
            isInRange = actual.CompareTo(_min) >= 0 && actual.CompareTo(_max) <= 0;
        }
        else
        {
            isInRange = actual.CompareTo(_min) > 0 && actual.CompareTo(_max) < 0;
        }

        if (isInRange == _shouldBeInRange)
        {
            return AssertionResult.Passed;
        }

        var rangeText = _inclusive ? $"[{_min}, {_max}]" : $"({_min}, {_max})";
        if (_shouldBeInRange)
        {
            return AssertionResult.Fail($"Expected {actual} to be between {rangeText}");
        }
        else
        {
            return AssertionResult.Fail($"Expected {actual} to not be between {rangeText}");
        }
    }
}

// Extension methods for range assertions
public static class BetweenAssertionExtensions
{
    public static BetweenAssertion<TActual> IsBetween<TActual>(this AssertionBuilder<TActual> builder, TActual min, TActual max, bool inclusive = true)
        where TActual : IComparable<TActual>
    {
        return new BetweenAssertion<TActual>(builder.ActualValueProvider, min, max, inclusive, shouldBeInRange: true);
    }

    public static BetweenAssertion<TActual> IsNotBetween<TActual>(this AssertionBuilder<TActual> builder, TActual min, TActual max, bool inclusive = true)
        where TActual : IComparable<TActual>
    {
        return new BetweenAssertion<TActual>(builder.ActualValueProvider, min, max, inclusive, shouldBeInRange: false);
    }
}