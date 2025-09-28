using System;
using System.Globalization;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for string parsing assertions
/// </summary>
public static class StringParseExtensions
{
    public static ParseAssertion<T> IsParsableInto<T>(this ValueAssertionBuilder<string> builder)
#if NET7_0_OR_GREATER
        where T : IParsable<T>
#endif
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new ParseAssertion<T>(nullableProvider, shouldBeParsable: true);
    }

    public static ParseAssertion<T> IsNotParsableInto<T>(this ValueAssertionBuilder<string> builder)
#if NET7_0_OR_GREATER
        where T : IParsable<T>
#endif
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new ParseAssertion<T>(nullableProvider, shouldBeParsable: false);
    }

    public static ValueAssertionBuilder<T> WhenParsedInto<T>(this ValueAssertionBuilder<string> builder)
#if NET7_0_OR_GREATER
        where T : IParsable<T>
#endif
    {
        return new ValueAssertionBuilder<T>(async () =>
        {
            var stringValue = await builder.ActualValueProvider();
            if (stringValue == null)
            {
                throw new InvalidOperationException($"Cannot parse null string to {typeof(T).Name}");
            }

#if NET7_0_OR_GREATER
            if (!T.TryParse(stringValue, CultureInfo.InvariantCulture, out var result))
            {
                throw new InvalidOperationException($"Failed to parse '{stringValue}' as {typeof(T).Name}");
            }
            return result;
#else
            return ParseValue<T>(stringValue);
#endif
        });
    }

    public static ParseAssertion<T> IsParsableInto<T>(this DualAssertionBuilder<string> builder)
#if NET7_0_OR_GREATER
        where T : IParsable<T>
#endif
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new ParseAssertion<T>(nullableProvider, shouldBeParsable: true);
    }

    public static ParseAssertion<T> IsNotParsableInto<T>(this DualAssertionBuilder<string> builder)
#if NET7_0_OR_GREATER
        where T : IParsable<T>
#endif
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new ParseAssertion<T>(nullableProvider, shouldBeParsable: false);
    }

    public static ValueAssertionBuilder<T> WhenParsedInto<T>(this DualAssertionBuilder<string> builder)
#if NET7_0_OR_GREATER
        where T : IParsable<T>
#endif
    {
        return new ValueAssertionBuilder<T>(async () =>
        {
            var stringValue = await builder.ActualValueProvider();
            if (stringValue == null)
            {
                throw new InvalidOperationException($"Cannot parse null string to {typeof(T).Name}");
            }

#if NET7_0_OR_GREATER
            if (!T.TryParse(stringValue, CultureInfo.InvariantCulture, out var result))
            {
                throw new InvalidOperationException($"Failed to parse '{stringValue}' as {typeof(T).Name}");
            }
            return result;
#else
            return ParseValue<T>(stringValue);
#endif
        });
    }

#if !NET7_0_OR_GREATER
    private static T ParseValue<T>(string value)
    {
        var type = typeof(T);
        if (type == typeof(int))
            return (T)(object)int.Parse(value);
        if (type == typeof(double))
            return (T)(object)double.Parse(value);
        if (type == typeof(float))
            return (T)(object)float.Parse(value);
        if (type == typeof(long))
            return (T)(object)long.Parse(value);
        if (type == typeof(bool))
            return (T)(object)bool.Parse(value);
        if (type == typeof(DateTime))
            return (T)(object)DateTime.Parse(value);
        if (type == typeof(DateTimeOffset))
            return (T)(object)DateTimeOffset.Parse(value);
        if (type == typeof(Guid))
            return (T)(object)Guid.Parse(value);
        if (type == typeof(decimal))
            return (T)(object)decimal.Parse(value);
        if (type == typeof(byte))
            return (T)(object)byte.Parse(value);
        if (type == typeof(short))
            return (T)(object)short.Parse(value);

        return (T)Convert.ChangeType(value, type);
    }
#endif

    public static CustomAssertion<string> IsParsableInto<T>(this ValueAssertionBuilder<string> builder, IFormatProvider? formatProvider)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            str =>
            {
                if (str == null) return false;

                if (typeof(T) == typeof(int))
                    return int.TryParse(str, NumberStyles.Any, formatProvider, out _);
                if (typeof(T) == typeof(double))
                    return double.TryParse(str, NumberStyles.Any, formatProvider, out _);
                if (typeof(T) == typeof(bool))
                    return bool.TryParse(str, out _);
                if (typeof(T) == typeof(DateTime))
                    return DateTime.TryParse(str, formatProvider, DateTimeStyles.None, out _);
                if (typeof(T) == typeof(Guid))
                    return Guid.TryParse(str, out _);

                return false;
            },
            $"Expected string to be parsable into {typeof(T).Name}");
    }
}