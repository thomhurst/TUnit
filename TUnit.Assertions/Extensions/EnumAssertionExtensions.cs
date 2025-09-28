using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for enum type assertions
/// </summary>
public static class EnumAssertionExtensions
{
    // === For ValueAssertionBuilder<TEnum> where TEnum : Enum ===
    public static ValueAssertionBuilder<TEnum> HasFlag<TEnum>(this ValueAssertionBuilder<TEnum> builder, TEnum flag)
        where TEnum : Enum
    {
        var assertion = new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                var actualValue = Convert.ToInt64(actual);
                var flagValue = Convert.ToInt64(flag);
                return (actualValue & flagValue) == flagValue;
            },
            $"Expected enum to have flag {flag}");

        // Return a new builder that chains the assertion
        return new ValueAssertionBuilder<TEnum>(async () =>
        {
            await assertion;
            return await builder.ActualValueProvider();
        });
    }

    public static ValueAssertionBuilder<TEnum> DoesNotHaveFlag<TEnum>(this ValueAssertionBuilder<TEnum> builder, TEnum flag)
        where TEnum : Enum
    {
        var assertion = new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return true;
                var actualValue = Convert.ToInt64(actual);
                var flagValue = Convert.ToInt64(flag);
                return (actualValue & flagValue) != flagValue;
            },
            $"Expected enum to not have flag {flag}");

        return new ValueAssertionBuilder<TEnum>(async () =>
        {
            await assertion;
            return await builder.ActualValueProvider();
        });
    }

    public static ValueAssertionBuilder<TEnum> IsDefined<TEnum>(this ValueAssertionBuilder<TEnum> builder)
        where TEnum : Enum
    {
        var assertion = new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual => actual != null && Enum.IsDefined(typeof(TEnum), actual),
            $"Expected enum value to be defined in {typeof(TEnum).Name}");

        return new ValueAssertionBuilder<TEnum>(async () =>
        {
            await assertion;
            return await builder.ActualValueProvider();
        });
    }

    public static ValueAssertionBuilder<TEnum> IsNotDefined<TEnum>(this ValueAssertionBuilder<TEnum> builder)
        where TEnum : Enum
    {
        var assertion = new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual => actual == null || !Enum.IsDefined(typeof(TEnum), actual),
            $"Expected enum value to not be defined in {typeof(TEnum).Name}");

        return new ValueAssertionBuilder<TEnum>(async () =>
        {
            await assertion;
            return await builder.ActualValueProvider();
        });
    }

    public static ValueAssertionBuilder<TEnum> HasSameNameAs<TEnum>(this ValueAssertionBuilder<TEnum> builder, Enum other)
        where TEnum : Enum
    {
        var assertion = new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual => actual?.ToString() == other?.ToString(),
            $"Expected enum to have same name as {other}");

        return new ValueAssertionBuilder<TEnum>(async () =>
        {
            await assertion;
            return await builder.ActualValueProvider();
        });
    }

    public static ValueAssertionBuilder<TEnum> DoesNotHaveSameNameAs<TEnum>(this ValueAssertionBuilder<TEnum> builder, Enum other)
        where TEnum : Enum
    {
        var assertion = new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual => actual?.ToString() != other?.ToString(),
            $"Expected enum to not have same name as {other}");

        return new ValueAssertionBuilder<TEnum>(async () =>
        {
            await assertion;
            return await builder.ActualValueProvider();
        });
    }

    public static ValueAssertionBuilder<TEnum> HasSameValueAs<TEnum>(this ValueAssertionBuilder<TEnum> builder, Enum other)
        where TEnum : Enum
    {
        var assertion = new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null && other == null) return true;
                if (actual == null || other == null) return false;
                return Convert.ToInt64(actual) == Convert.ToInt64(other);
            },
            $"Expected enum to have same value as {other}");

        return new ValueAssertionBuilder<TEnum>(async () =>
        {
            await assertion;
            return await builder.ActualValueProvider();
        });
    }

    public static ValueAssertionBuilder<TEnum> DoesNotHaveSameValueAs<TEnum>(this ValueAssertionBuilder<TEnum> builder, Enum other)
        where TEnum : Enum
    {
        var assertion = new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null && other == null) return false;
                if (actual == null || other == null) return true;
                return Convert.ToInt64(actual) != Convert.ToInt64(other);
            },
            $"Expected enum to not have same value as {other}");

        return new ValueAssertionBuilder<TEnum>(async () =>
        {
            await assertion;
            return await builder.ActualValueProvider();
        });
    }

    // === For DualAssertionBuilder<TEnum> where TEnum : Enum ===
    public static CustomAssertion<TEnum> HasFlag<TEnum>(this DualAssertionBuilder<TEnum> builder, TEnum flag)
        where TEnum : Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                var actualValue = Convert.ToInt64(actual);
                var flagValue = Convert.ToInt64(flag);
                return (actualValue & flagValue) == flagValue;
            },
            $"Expected enum to have flag {flag}");
    }

    public static CustomAssertion<TEnum> DoesNotHaveFlag<TEnum>(this DualAssertionBuilder<TEnum> builder, TEnum flag)
        where TEnum : Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return true;
                var actualValue = Convert.ToInt64(actual);
                var flagValue = Convert.ToInt64(flag);
                return (actualValue & flagValue) != flagValue;
            },
            $"Expected enum to not have flag {flag}");
    }

    public static CustomAssertion<TEnum> IsDefined<TEnum>(this DualAssertionBuilder<TEnum> builder)
        where TEnum : Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual => actual != null && Enum.IsDefined(typeof(TEnum), actual),
            $"Expected enum value to be defined in {typeof(TEnum).Name}");
    }

    public static CustomAssertion<TEnum> IsNotDefined<TEnum>(this DualAssertionBuilder<TEnum> builder)
        where TEnum : Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual => actual == null || !Enum.IsDefined(typeof(TEnum), actual),
            $"Expected enum value to not be defined in {typeof(TEnum).Name}");
    }

    public static CustomAssertion<TEnum> HasSameNameAs<TEnum>(this DualAssertionBuilder<TEnum> builder, Enum other)
        where TEnum : Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual => actual?.ToString() == other?.ToString(),
            $"Expected enum to have same name as {other}");
    }

    public static CustomAssertion<TEnum> DoesNotHaveSameNameAs<TEnum>(this DualAssertionBuilder<TEnum> builder, Enum other)
        where TEnum : Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            actual => actual?.ToString() != other?.ToString(),
            $"Expected enum to not have same name as {other}");
    }
}