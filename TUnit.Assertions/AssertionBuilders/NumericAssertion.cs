using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Numeric assertions for integer values
/// </summary>
public class NumericAssertion<T> : AssertionBase<T>
    where T : struct, IComparable<T>
{
    public NumericAssertion(Func<Task<T>> actualValueProvider)
        : base(actualValueProvider)
    {
    }

    public NumericAssertion(Func<T> actualValueProvider)
        : base(actualValueProvider)
    {
    }

    public CustomAssertion<T> Positive()
    {
        return new CustomAssertion<T>(GetActualValueAsync,
            value => value.CompareTo(default(T)) > 0,
            $"Expected positive value but was {{ActualValue}}");
    }

    public CustomAssertion<T> Negative()
    {
        return new CustomAssertion<T>(GetActualValueAsync,
            value => value.CompareTo(default(T)) < 0,
            $"Expected negative value but was {{ActualValue}}");
    }

    public CustomAssertion<T> Zero()
    {
        return new CustomAssertion<T>(GetActualValueAsync,
            value => value.CompareTo(default(T)) == 0,
            $"Expected zero but was {{ActualValue}}");
    }

    public CustomAssertion<T> NotZero()
    {
        return new CustomAssertion<T>(GetActualValueAsync,
            value => value.CompareTo(default(T)) != 0,
            $"Expected non-zero value but was {{ActualValue}}");
    }

    public CustomAssertion<T> GreaterThan(T other)
    {
        return new CustomAssertion<T>(GetActualValueAsync,
            value => value.CompareTo(other) > 0,
            $"Expected value greater than {other} but was {{ActualValue}}");
    }

    public CustomAssertion<T> LessThan(T other)
    {
        return new CustomAssertion<T>(GetActualValueAsync,
            value => value.CompareTo(other) < 0,
            $"Expected value less than {other} but was {{ActualValue}}");
    }

    public CustomAssertion<T> GreaterThanOrEqualTo(T other)
    {
        return new CustomAssertion<T>(GetActualValueAsync,
            value => value.CompareTo(other) >= 0,
            $"Expected value greater than or equal to {other} but was {{ActualValue}}");
    }

    public CustomAssertion<T> LessThanOrEqualTo(T other)
    {
        return new CustomAssertion<T>(GetActualValueAsync,
            value => value.CompareTo(other) <= 0,
            $"Expected value less than or equal to {other} but was {{ActualValue}}");
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        await GetActualValueAsync();
        return AssertionResult.Passed;
    }
}