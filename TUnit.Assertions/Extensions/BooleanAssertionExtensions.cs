using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for boolean type assertions
/// </summary>
public static class BooleanAssertionExtensions
{
    // For ValueAssertionBuilder<bool>
    public static BooleanAssertion IsTrue(this ValueAssertionBuilder<bool> builder)
    {
        return new BooleanAssertion(builder.ActualValueProvider, expectedValue: true);
    }

    public static BooleanAssertion IsFalse(this ValueAssertionBuilder<bool> builder)
    {
        return new BooleanAssertion(builder.ActualValueProvider, expectedValue: false);
    }

    // For DualAssertionBuilder<bool> (value-returning delegates)
    public static BooleanAssertion IsTrue(this DualAssertionBuilder<bool> builder)
    {
        return new BooleanAssertion(builder.ActualValueProvider, expectedValue: true);
    }

    public static BooleanAssertion IsFalse(this DualAssertionBuilder<bool> builder)
    {
        return new BooleanAssertion(builder.ActualValueProvider, expectedValue: false);
    }

    // For nullable bool ValueAssertionBuilder<bool?>
    public static BooleanAssertion IsTrue(this ValueAssertionBuilder<bool?> builder)
    {
        return new BooleanAssertion(async () =>
        {
            var value = await builder.ActualValueProvider();
            if (!value.HasValue)
                throw new InvalidOperationException("Cannot assert IsTrue on null boolean value");
            return value.Value;
        }, expectedValue: true);
    }

    public static BooleanAssertion IsFalse(this ValueAssertionBuilder<bool?> builder)
    {
        return new BooleanAssertion(async () =>
        {
            var value = await builder.ActualValueProvider();
            if (!value.HasValue)
                throw new InvalidOperationException("Cannot assert IsFalse on null boolean value");
            return value.Value;
        }, expectedValue: false);
    }

    // For DualAssertionBuilder<bool?> (nullable bool returning delegates)
    public static BooleanAssertion IsTrue(this DualAssertionBuilder<bool?> builder)
    {
        return new BooleanAssertion(async () =>
        {
            var value = await builder.ActualValueProvider();
            if (!value.HasValue)
                throw new InvalidOperationException("Cannot assert IsTrue on null boolean value");
            return value.Value;
        }, expectedValue: true);
    }

    public static BooleanAssertion IsFalse(this DualAssertionBuilder<bool?> builder)
    {
        return new BooleanAssertion(async () =>
        {
            var value = await builder.ActualValueProvider();
            if (!value.HasValue)
                throw new InvalidOperationException("Cannot assert IsFalse on null boolean value");
            return value.Value;
        }, expectedValue: false);
    }
}